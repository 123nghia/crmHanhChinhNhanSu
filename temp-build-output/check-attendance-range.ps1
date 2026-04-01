$connectionString = 'Server=localhost;Database=humandev;Trusted_Connection=True;TrustServerCertificate=True;'
$query = @"
SELECT 
    COUNT(*) AS AttendanceRecordCount,
    MIN(CAST(WorkDate AS date)) AS EarliestAttendanceDate,
    MAX(CAST(WorkDate AS date)) AS LatestAttendanceDate,
    COUNT(DISTINCT CAST(WorkDate AS date)) AS AttendanceDayCount,
    MAX(UpdateAt) AS LastAttendanceUpdateAt
FROM dbo.AttendanceRecords;
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
$table | Format-Table -AutoSize | Out-String -Width 4096
