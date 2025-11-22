-- =============================================
-- Script test connection và stored procedure
-- Chạy trong SSMS để kiểm tra
-- =============================================

-- 1. Test connection
SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName;

-- 2. Kiểm tra stored procedure có tồn tại không
SELECT 
    name,
    type_desc,
    create_date,
    modify_date
FROM sys.objects 
WHERE name = 'sp_Employee_getAll' 
AND type = 'P';

-- 3. Kiểm tra bảng Employees có dữ liệu không
SELECT COUNT(*) AS TotalEmployees FROM Employees;
SELECT TOP 5 * FROM Employees;

-- 4. Test stored procedure với parameters mặc định
EXEC sp_Employee_getAll
    @Token = '',
    @OrderBy = '',
    @userId = NULL,
    @MemberId = NULL,
    @GroupId = NULL,
    @Page = 1,
    @Status = -1,
    @Limit = 10,
    @From = NULL,
    @To = NULL,
    @IsDeleted = 0;

-- 5. Kiểm tra các functions được gọi trong SP
SELECT name FROM sys.objects 
WHERE name IN ('getDisplayMasterData', 'getDisplayMasterdata', 'getFullName')
AND type = 'FN';

-- 6. Kiểm tra lỗi trong SP (nếu có)
SELECT 
    OBJECT_NAME(object_id) AS ObjectName,
    error_number,
    error_severity,
    error_state,
    error_message
FROM sys.dm_exec_requests
WHERE session_id = @@SPID
AND error_number IS NOT NULL;

