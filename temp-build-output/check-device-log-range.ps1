$connectionString = 'Server=localhost;Database=humandev;Trusted_Connection=True;TrustServerCertificate=True;'
$query = @"
SELECT 
    COUNT(*) AS DeviceLogCount,
    MIN(RecordTime) AS EarliestDeviceLog,
    MAX(RecordTime) AS LatestDeviceLog,
    MAX(CreateAt) AS LastDeviceLogCachedAt
FROM dbo.AttendanceDeviceLogs;
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
