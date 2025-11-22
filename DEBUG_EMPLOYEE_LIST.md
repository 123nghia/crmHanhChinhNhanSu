# 🔍 Debug: Không lấy được danh sách nhân viên

## ⚠️ Vấn đề
Không lấy được danh sách nhân viên mặc dù có dữ liệu trong database.

## 🔧 Các bước kiểm tra

### 1. Kiểm tra Connection String
✅ Connection string đã đúng trong `appsettings.json`:
```
Server=115.79.5.244,1433
Database=humandev
User ID=crm
Password=Vietstar@2018
```

### 2. Chạy script test trong SSMS
Mở file: `TEST_CONNECTION_AND_SP.sql` và chạy trong SQL Server Management Studio

Script sẽ:
- ✅ Kiểm tra connection
- ✅ Kiểm tra stored procedure `sp_Employee_getAll` có tồn tại
- ✅ Kiểm tra bảng `Employees` có dữ liệu
- ✅ Test stored procedure với parameters mặc định
- ✅ Kiểm tra các functions được gọi

### 3. Kiểm tra lỗi trong code

Đã thêm logging vào `RepositoryBase.cs`:
- Log stored procedure name
- Log parameters
- Log kết quả query
- Log exception nếu có

**Xem logs khi chạy application:**
```bash
dotnet run --project crmHuman
```

Tìm trong output:
```
Executing stored procedure: sp_Employee_getAll
Parameters: {...}
Query result: Total=..., Count=...
Error in GetBaseAll: ... (nếu có lỗi)
```

### 4. Các nguyên nhân có thể

#### A. Stored Procedure không tồn tại
```sql
-- Kiểm tra
SELECT * FROM sys.objects 
WHERE name = 'sp_Employee_getAll' AND type = 'P';
```

**Fix:** Chạy migration V002 hoặc chạy lại `sqlscript.sql`

#### B. Functions không tồn tại
Stored procedure gọi các functions:
- `getDisplayMasterData`
- `getDisplayMasterdata` (có thể typo - thiếu chữ "a")
- `getFullName`
- `getAllUserByUserId`

**Kiểm tra:**
```sql
SELECT name FROM sys.objects 
WHERE name IN ('getDisplayMasterData', 'getDisplayMasterdata', 'getFullName', 'getAllUserByUserId')
AND type = 'FN';
```

#### C. Lỗi trong stored procedure
Có thể có lỗi SQL syntax trong SP.

**Test trực tiếp:**
```sql
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
```

#### D. Filter IsDeleted
Code có logic:
```csharp
if (UserData.RoleCode == "1") {
    request2.IsDeleted = true;  // Admin: xem cả deleted
} else {
    request2.IsDeleted = false; // User khác: chỉ xem active
}
```

**Kiểm tra:**
- User đang login có RoleCode là gì?
- Có nhân viên nào có `Deleted = 0` không?

```sql
-- Kiểm tra nhân viên active
SELECT COUNT(*) FROM Employees WHERE ISNULL(Deleted, 0) = 0;

-- Kiểm tra nhân viên deleted
SELECT COUNT(*) FROM Employees WHERE ISNULL(Deleted, 0) = 1;
```

#### E. Exception bị catch và trả về empty
Code đang catch exception và trả về empty list:
```csharp
catch (Exception e) {
    return new BaseList() {
        Data = new List<Object>(),
        Total = 0,
    };
}
```

**Fix:** Đã thêm logging, xem output để biết lỗi cụ thể.

### 5. Quick Fix

#### Option 1: Test trực tiếp stored procedure
```sql
-- Chạy trong SSMS
EXEC sp_Employee_getAll @IsDeleted = 0;
```

Nếu lỗi → Fix stored procedure
Nếu OK → Vấn đề ở code C#

#### Option 2: Test connection từ code
Thêm vào `Employee.cshtml.cs`:
```csharp
try {
    using var con = new SqlConnection(_configuration.GetConnectionString("stringConnect7"));
    await con.OpenAsync();
    var test = await con.QueryAsync("SELECT TOP 1 * FROM Employees");
    _logger.LogInformation($"Connection OK. Test query returned {test.Count()} rows");
} catch (Exception ex) {
    _logger.LogError($"Connection failed: {ex.Message}");
}
```

## 📋 Checklist

- [ ] Chạy `TEST_CONNECTION_AND_SP.sql` trong SSMS
- [ ] Kiểm tra stored procedure có tồn tại
- [ ] Kiểm tra functions có tồn tại
- [ ] Test stored procedure trực tiếp
- [ ] Kiểm tra dữ liệu trong bảng Employees
- [ ] Xem logs khi chạy application
- [ ] Kiểm tra RoleCode của user đang login
- [ ] Kiểm tra filter IsDeleted

## 🚀 Next Steps

1. Chạy script test trong SSMS
2. Xem kết quả và báo lại lỗi cụ thể (nếu có)
3. Fix theo lỗi được báo

