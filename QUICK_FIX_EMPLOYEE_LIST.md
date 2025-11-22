# 🔧 Quick Fix: Không lấy được danh sách nhân viên

## ✅ Từ hình ảnh SSMS:

- ✅ Stored procedure `sp_Employee_getAll` **TỒN TẠI**
- ✅ Có **45 nhân viên** trong database
- ✅ Query **CHẠY ĐƯỢC** và trả về dữ liệu

## 🔍 Vấn đề có thể:

### 1. Typo trong Stored Procedure

Trong `sqlscript.sql` dòng 3097-3098:
```sql
dbo.getDisplayMasterdata(d.DepartmentCode)  -- ❌ Thiếu chữ "a"
dbo.getDisplayMasterdata(d.PositionCode)    -- ❌ Thiếu chữ "a"
```

Function thực tế: `getDisplayMasterData` (có chữ "a")

**Fix:** Đã tạo migration V005 để sửa typo này.

### 2. Exception bị catch và trả về empty

Code đang catch exception và trả về empty list mà không log rõ ràng.

**Fix:** Đã cải thiện logging - sẽ hiển thị lỗi trong console.

## 🚀 Cách fix:

### Bước 1: Chạy migration V005

```bash
dotnet run --project crmHuman
```

Migration V005 sẽ tự động:
- Drop stored procedure cũ
- Tạo lại với function names đúng

### Bước 2: Xem logs khi chạy

```bash
dotnet run --project crmHuman
```

Tìm trong output:
```
[ERROR] Error in GetBaseAll - SP: sp_Employee_getAll, Error: ...
```

### Bước 3: Test lại

Sau khi migration chạy xong, test lại trang Employee list.

## 📋 Checklist:

- [ ] Chạy `dotnet run` để apply migration V005
- [ ] Kiểm tra logs có lỗi gì không
- [ ] Test lại trang Employee list
- [ ] Nếu vẫn lỗi, xem error message cụ thể trong logs

## 🔍 Nếu vẫn không được:

Kiểm tra trong SSMS:
```sql
-- Test function có tồn tại không
SELECT name FROM sys.objects 
WHERE name IN ('getDisplayMasterData', 'getDisplayMasterdata')
AND type = 'FN';

-- Test stored procedure sau khi fix
EXEC sp_Employee_getAll @IsDeleted = 0;
```

