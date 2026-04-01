$connectionString = 'Server=localhost;Database=humandev;Trusted_Connection=True;TrustServerCertificate=True;'
$query = @"
IF OBJECT_ID('dbo.AttendanceSyncStates', 'U') IS NULL
BEGIN
    SELECT CAST(0 AS bit) AS HasTable;
END
ELSE
BEGIN
    SELECT TOP 10 SyncKey, RangeStartDate, RangeEndDate, IsCompleted, LastRunAt, LastSuccessAt, LastMessage
    FROM dbo.AttendanceSyncStates
    ORDER BY LastRunAt DESC;
END
"@
Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$command = $connection.CreateCommand()
$command.CommandText = $query
$connection.Open()
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
$table = New-Object System.Data.DataTable
[void]$adapter.Fill($table)
$connection.Close()
if ($table.Rows.Count -eq 0) { 'NO_ROWS' } else { $table | Format-Table -AutoSize | Out-String -Width 4096 }
