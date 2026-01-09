using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace crmHuman.Services
{
    /// <summary>
    /// Service để tự động chạy database migration khi ứng dụng khởi động
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseMigrationService> _logger;
        private readonly IWebHostEnvironment _environment;
        private const string ConnectionStringName = "stringConnect7";
        private const string MigrationsDirectory = "migrations";

        public DatabaseMigrationService(
            IConfiguration configuration, 
            ILogger<DatabaseMigrationService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Chạy migration tự động từ thư mục migrations/
        /// </summary>
        public async Task RunMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("=== Bắt đầu chạy database migration ===");

                var connectionString = _configuration.GetConnectionString(ConnectionStringName);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Không tìm thấy connection string: {ConnectionStringName}", ConnectionStringName);
                    return;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Đảm bảo bảng __MigrationHistory tồn tại
                await EnsureMigrationHistoryTableAsync(connection);

                // Lấy danh sách migrations đã chạy
                var appliedMigrations = await GetAppliedMigrationsAsync(connection);
                _logger.LogInformation("Đã tìm thấy {Count} migration(s) đã được áp dụng", appliedMigrations.Count);

                // Đọc tất cả file migration
                var migrationFiles = GetMigrationFiles();
                if (migrationFiles.Count == 0)
                {
                    _logger.LogWarning("Không tìm thấy file migration nào trong thư mục '{Directory}'", MigrationsDirectory);
                    return;
                }

                _logger.LogInformation("Tìm thấy {Count} file migration trong thư mục '{Directory}'", 
                    migrationFiles.Count, MigrationsDirectory);

                // Chạy các migration chưa được áp dụng
                var pendingMigrations = migrationFiles
                    .Where(m => !appliedMigrations.Contains(m.Version))
                    .OrderBy(m => m.Version)
                    .ToList();

                if (pendingMigrations.Count == 0)
                {
                    _logger.LogInformation("Tất cả migrations đã được áp dụng. Không có migration mới cần chạy.");
                }
                else
                {
                    _logger.LogInformation("Có {Count} migration(s) pending cần chạy", pendingMigrations.Count);

                    foreach (var migration in pendingMigrations)
                    {
                        await ApplyMigrationAsync(connection, migration);
                    }
                }

                _logger.LogInformation("=== Database migration hoàn thành thành công! ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy database migration");
                // Không throw exception để ứng dụng vẫn có thể khởi động
            }
        }

        /// <summary>
        /// Đảm bảo bảng __MigrationHistory tồn tại
        /// </summary>
        private async Task EnsureMigrationHistoryTableAsync(SqlConnection connection)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__MigrationHistory')
                BEGIN
                    CREATE TABLE [dbo].[__MigrationHistory](
                        [MigrationId] INT IDENTITY(1,1) PRIMARY KEY,
                        [Version] VARCHAR(10) NOT NULL UNIQUE,
                        [Description] NVARCHAR(255) NOT NULL,
                        [FileName] VARCHAR(255) NOT NULL,
                        [AppliedOn] DATETIME2 NOT NULL DEFAULT GETDATE(),
                        [ExecutionTime] INT NULL,
                        [Success] BIT NOT NULL DEFAULT 1,
                        [ErrorMessage] NVARCHAR(MAX) NULL
                    );
                END";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy danh sách version của các migration đã chạy
        /// </summary>
        private async Task<HashSet<string>> GetAppliedMigrationsAsync(SqlConnection connection)
        {
            var appliedMigrations = new HashSet<string>();

            var sql = "SELECT Version FROM __MigrationHistory WHERE Success = 1";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                appliedMigrations.Add(reader.GetString(0));
            }

            return appliedMigrations;
        }

        /// <summary>
        /// Lấy danh sách file migration từ thư mục migrations/
        /// </summary>
        private List<MigrationFile> GetMigrationFiles()
        {
            var migrations = new List<MigrationFile>();

            // Tìm thư mục migrations - thử cả ContentRootPath và parent directory
            var contentRoot = _environment.ContentRootPath;
            var migrationPath = Path.Combine(contentRoot, MigrationsDirectory);
            
            _logger.LogInformation("ContentRootPath: {ContentRoot}", contentRoot);
            _logger.LogInformation("Đang tìm thư mục migration tại: {Path}", migrationPath);
            
            if (!Directory.Exists(migrationPath))
            {
                // Thử tìm ở parent directory (project root)
                var parentPath = Directory.GetParent(contentRoot)?.FullName;
                if (parentPath != null)
                {
                    migrationPath = Path.Combine(parentPath, MigrationsDirectory);
                    _logger.LogInformation("Thử tìm ở parent directory: {Path}", migrationPath);
                }
                
                if (!Directory.Exists(migrationPath))
                {
                    _logger.LogWarning("Thư mục migration không tồn tại: {Path}", migrationPath);
                    _logger.LogWarning("Vui lòng đảm bảo thư mục 'migrations' nằm ở project root");
                    return migrations;
                }
            }

            // Đọc tất cả file .sql
            var files = Directory.GetFiles(migrationPath, "V*.sql", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var match = Regex.Match(fileName, @"^V(\d{3})__(.+)\.sql$");

                if (match.Success)
                {
                    migrations.Add(new MigrationFile
                    {
                        Version = match.Groups[1].Value,
                        Description = match.Groups[2].Value.Replace("_", " "),
                        FileName = fileName,
                        FilePath = file
                    });
                }
                else
                {
                    _logger.LogWarning("File migration không đúng format: {FileName}", fileName);
                }
            }

            return migrations;
        }

        /// <summary>
        /// Áp dụng một migration
        /// </summary>
        private async Task ApplyMigrationAsync(SqlConnection connection, MigrationFile migration)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Đang áp dụng migration {Version}: {Description}...", 
                migration.Version, migration.Description);

            SqlTransaction? transaction = null;
            try
            {
                // Bắt đầu transaction cho mỗi file migration
                transaction = connection.BeginTransaction();

                // Đọc nội dung SQL
                var sql = await File.ReadAllTextAsync(migration.FilePath);

                // Tách SQL thành các batch bằng lệnh GO
                var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;

                    using var command = new SqlCommand(batch, connection, transaction);
                    command.CommandTimeout = 300; // 5 minutes timeout
                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                stopwatch.Stop();

                // Ghi log vào __MigrationHistory (Ghi trên connect mới vì transaction cũ đã đóng)
                // Hoặc bỏ qua transaction cho phần record này nêú không cần thiết
                await RecordMigrationAsync(connection, migration, stopwatch.ElapsedMilliseconds, true, null);

                _logger.LogInformation("Migration {Version} hoàn thành trong {Time}ms", 
                    migration.Version, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Lỗi khi áp dụng migration {Version}: {Description}", 
                    migration.Version, migration.Description);

                try
                {
                    transaction?.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Lỗi khi rollback transaction cho migration {Version}", migration.Version);
                }

                // Ghi log lỗi vào __MigrationHistory
                await RecordMigrationAsync(connection, migration, stopwatch.ElapsedMilliseconds, false, ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Ghi log migration vào bảng __MigrationHistory
        /// </summary>
        private async Task RecordMigrationAsync(
            SqlConnection connection, 
            MigrationFile migration, 
            long executionTime, 
            bool success, 
            string? errorMessage)
        {
            var sql = @"
                IF EXISTS (SELECT 1 FROM __MigrationHistory WHERE Version = @Version)
                BEGIN
                    UPDATE __MigrationHistory 
                    SET Description = @Description, 
                        FileName = @FileName, 
                        ExecutionTime = @ExecutionTime, 
                        Success = @Success, 
                        ErrorMessage = @ErrorMessage,
                        AppliedOn = GETDATE()
                    WHERE Version = @Version
                END
                ELSE
                BEGIN
                    INSERT INTO __MigrationHistory 
                    (Version, Description, FileName, ExecutionTime, Success, ErrorMessage)
                    VALUES 
                    (@Version, @Description, @FileName, @ExecutionTime, @Success, @ErrorMessage)
                END";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Version", migration.Version);
            command.Parameters.AddWithValue("@Description", migration.Description);
            command.Parameters.AddWithValue("@FileName", migration.FileName);
            command.Parameters.AddWithValue("@ExecutionTime", executionTime);
            command.Parameters.AddWithValue("@Success", success);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Represents a migration file
        /// </summary>
        private class MigrationFile
        {
            public string Version { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
        }
    }
}

