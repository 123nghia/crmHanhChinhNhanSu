# Hướng dẫn Migration Database

## Tổng quan
Script migration này thêm các trường mới vào database để hỗ trợ các tính năng mới của hệ thống quản lý nhân viên.

## Các trường mới được thêm

### Bảng Employees
1. **Gender** (NVARCHAR(50)) - Giới tính
2. **PlaceOfBirth** (NVARCHAR(255)) - Nơi sinh
3. **Religion** (NVARCHAR(100)) - Tôn giáo
4. **PersonalEmail** (NVARCHAR(255)) - Email cá nhân
5. **BeneficiaryName** (NVARCHAR(255)) - Tên chủ tài khoản

### Bảng TaxItem
1. **PITDate** (DATETIME) - Ngày cấp mã số thuế
2. **EffectedFrom** (DATETIME) - Hiệu lực từ

## Cách thực hiện Migration

### Bước 1: Backup Database
**QUAN TRỌNG**: Trước khi chạy migration, hãy backup database của bạn!

```sql
-- Backup database
BACKUP DATABASE [YourDatabaseName] 
TO DISK = 'C:\Backup\YourDatabaseName_Backup.bak'
WITH FORMAT, COMPRESSION;
```

### Bước 2: Chạy Script Migration

1. Mở SQL Server Management Studio (SSMS) hoặc công cụ SQL client của bạn
2. Kết nối đến database của bạn
3. Mở file `MIGRATION_ADD_NEW_FIELDS.sql`
4. **Thay đổi tên database** trong dòng đầu tiên:
   ```sql
   USE [YourDatabaseName]  -- Thay [YourDatabaseName] bằng tên database thực tế
   ```
5. Chạy script (F5 hoặc Execute)

### Bước 3: Cập nhật Stored Procedures

**QUAN TRỌNG**: Sau khi chạy migration script, bạn **PHẢI** chạy script cập nhật stored procedures!

**Cách 1: Sử dụng script tổng hợp (Khuyến nghị)**
1. Mở file `docs/UPDATE_ALL_STORED_PROCEDURES.sql`
2. **Thay đổi tên database** trong dòng đầu tiên:
   ```sql
   USE [YourDatabaseName]  -- Thay [YourDatabaseName] bằng tên database thực tế
   ```
3. Chạy script (F5 hoặc Execute)

Script này sẽ cập nhật:
- ✅ `sp_emp_insert` với 5 tham số mới: `@Gender`, `@PlaceOfBirth`, `@Religion`, `@PersonalEmail`, `@BeneficiaryName`
- ✅ `sp_emp_update` với các tham số mới tương tự

**Cách 2: Sử dụng script riêng lẻ**
- `docs/UPDATE_SP_EMP_INSERT_ACTUAL.sql` - Chỉ cập nhật sp_emp_insert
- `docs/UPDATE_SP_EMP_UPDATE_ACTUAL.sql` - Chỉ cập nhật sp_emp_update
- `docs/UPDATE_STORED_PROCEDURES.sql` - Script generic với dynamic SQL (nếu cần)

**Lưu ý**: 
- Script `UPDATE_ALL_STORED_PROCEDURES.sql` dựa trên cấu trúc stored procedure thực tế của bạn
- Nếu bạn cần cập nhật `sp_tax_insert` và `sp_tax_udpate`, hãy sử dụng `UPDATE_STORED_PROCEDURES.sql`

### Bước 4: Kiểm tra kết quả

Sau khi chạy script, kiểm tra:

1. Các cột đã được thêm vào bảng:
   ```sql
   -- Kiểm tra bảng Employees
   SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'Employees' 
   AND COLUMN_NAME IN ('Gender', 'PlaceOfBirth', 'Religion', 'PersonalEmail', 'BeneficiaryName');
   
   -- Kiểm tra bảng TaxItem
   SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'TaxItem' 
   AND COLUMN_NAME IN ('PITDate', 'EffectedFrom');
   ```

2. Test các stored procedures với dữ liệu mẫu

## Lưu ý quan trọng

1. **Tất cả các cột mới đều là NULLABLE** - không bắt buộc, cho phép dữ liệu cũ vẫn hoạt động bình thường
2. **Script có kiểm tra tồn tại** - có thể chạy lại an toàn mà không gây lỗi
3. **Cần cập nhật stored procedures** - nếu không cập nhật, các thao tác INSERT/UPDATE sẽ bị lỗi
4. **Kiểm tra tên bảng** - đảm bảo tên bảng trong script khớp với database thực tế (có thể là `Employee` thay vì `Employees`)

## Rollback (Nếu cần)

Nếu cần rollback, bạn có thể xóa các cột:

```sql
-- Xóa các cột từ bảng Employees
ALTER TABLE [dbo].[Employees] DROP COLUMN [Gender];
ALTER TABLE [dbo].[Employees] DROP COLUMN [PlaceOfBirth];
ALTER TABLE [dbo].[Employees] DROP COLUMN [Religion];
ALTER TABLE [dbo].[Employees] DROP COLUMN [PersonalEmail];
ALTER TABLE [dbo].[Employees] DROP COLUMN [BeneficiaryName];

-- Xóa các cột từ bảng TaxItem
ALTER TABLE [dbo].[TaxItem] DROP COLUMN [PITDate];
ALTER TABLE [dbo].[TaxItem] DROP COLUMN [EffectedFrom];
```

**LƯU Ý**: Chỉ rollback nếu chắc chắn và đã backup database!

## Troubleshooting

### Lỗi: "Invalid object name 'Employees'"
- Kiểm tra tên bảng thực tế trong database của bạn
- Có thể là `Employee` (số ít) thay vì `Employees` (số nhiều)
- Cập nhật script với tên bảng đúng

### Lỗi: "Cannot insert/update because stored procedure doesn't have parameter"
- Bạn chưa cập nhật stored procedures
- Tham khảo phần "Bước 3" ở trên để cập nhật

### Lỗi: "Column already exists"
- Cột đã được thêm trước đó
- Script sẽ bỏ qua và tiếp tục, không ảnh hưởng

## Hỗ trợ

Nếu gặp vấn đề, vui lòng:
1. Kiểm tra log SQL Server
2. Xem lại cấu trúc database hiện tại
3. Đảm bảo đã backup trước khi thực hiện

