# 🎯 Hệ thống Database Migration - Tóm tắt

## ✅ Đã hoàn thành

### 1. Cấu trúc thư mục

```
blockchainHC/
├── migrations/                                   # ⭐ MỚI
│   ├── README.md                                # Hướng dẫn đầy đủ
│   ├── CHANGELOG.md                             # Lịch sử thay đổi
│   ├── V001__Initial_Schema.sql                # Migration 1: Tạo __MigrationHistory
│   ├── V002__Add_Employee_Extended_Fields.sql  # Migration 2: Thêm fields mới
│   └── V003__Update_Stored_Procedures.sql      # Migration 3: Cập nhật SPs
│
├── crmHuman/Services/
│   └── DatabaseMigrationService.cs             # ⭐ ĐÃ CẬP NHẬT - Tự động chạy migrations
│
├── docs/
│   └── DATABASE_MIGRATION_SYSTEM.md            # ⭐ MỚI - Tài liệu chi tiết
│
└── sqlscript.sql                                # Base schema (reference only)
```

### 2. Bảng __MigrationHistory

Tự động được tạo để track migrations:

| Column | Type | Description |
|--------|------|-------------|
| MigrationId | INT | Primary key |
| Version | VARCHAR(10) | Version số (001, 002, ...) |
| Description | NVARCHAR(255) | Mô tả migration |
| FileName | VARCHAR(255) | Tên file |
| AppliedOn | DATETIME2 | Thời gian chạy |
| ExecutionTime | INT | Thời gian thực thi (ms) |
| Success | BIT | Thành công hay không |
| ErrorMessage | NVARCHAR(MAX) | Chi tiết lỗi (nếu có) |

### 3. Migration Service

`DatabaseMigrationService.cs` đã được cập nhật:
- ✅ Đọc tất cả file migration từ thư mục `migrations/`
- ✅ Parse version từ tên file (format: `V{version}__{description}.sql`)
- ✅ So sánh với `__MigrationHistory` để tìm pending migrations
- ✅ Chạy migrations theo thứ tự version
- ✅ Ghi log execution time và errors
- ✅ Tự động chạy khi application start

## 📋 Workflow sử dụng

### Khi cần thay đổi database:

#### 1. Tạo migration file mới

```bash
# Trong thư mục migrations/, tạo file:
# Format: V{next_version}__{description}.sql

# Ví dụ:
migrations/V004__Add_Department_Table.sql
```

#### 2. Viết SQL script

```sql
-- =============================================
-- Migration: V004__Add_Department_Table
-- Author: Your Name
-- Date: 2024-11-22
-- Description: Thêm bảng Department
-- =============================================

BEGIN TRANSACTION;

PRINT 'Creating Department table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Department')
BEGIN
    CREATE TABLE [dbo].[Department](
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Code] VARCHAR(10) NOT NULL UNIQUE,
        [Name] NVARCHAR(100) NOT NULL,
        [CreatedAt] DATETIME2 DEFAULT GETDATE()
    );
END

PRINT 'Department table created successfully.';

COMMIT TRANSACTION;
GO
```

#### 3. Test local

```bash
# Chạy application → migration tự động execute
# Hoặc test manual:
sqlcmd -S localhost -d humandev -i migrations/V004__Add_Department_Table.sql
```

#### 4. Kiểm tra kết quả

```sql
-- Xem migration đã chạy
SELECT * FROM __MigrationHistory ORDER BY Version;

-- Check table đã được tạo
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Department';
```

#### 5. Commit vào Git

```bash
git add migrations/V004__Add_Department_Table.sql
git commit -m "feat: add Department table migration"
git push
```

#### 6. Update CHANGELOG

```bash
# Update migrations/CHANGELOG.md
## [V004] - 2024-11-22

### Added
- Bảng Department với các cột Id, Code, Name, CreatedAt
```

## 🎯 Quy tắc quan trọng

### ✅ Luôn làm:

1. **Đặt tên đúng format**: `V{version}__{description}.sql`
2. **Version 3 chữ số**: V001, V002, ..., V010, V011, ...
3. **Idempotent scripts**: Có thể chạy nhiều lần không lỗi
4. **Transaction**: Wrap trong BEGIN TRANSACTION / COMMIT
5. **PRINT statements**: Log từng bước thực hiện
6. **Test local** trước khi commit
7. **Update CHANGELOG** sau khi tạo migration mới

### ❌ Không bao giờ:

1. ❌ Sửa migration đã chạy trên production
2. ❌ Xóa migration file đã apply
3. ❌ Thay đổi version number của migration có sẵn
4. ❌ Skip version (V001, V003 → thiếu V002)
5. ❌ Sử dụng data cụ thể trong migration
6. ❌ Quên test trước khi commit

## 📊 Monitoring

### Xem tất cả migrations

```sql
SELECT 
    Version,
    Description,
    AppliedOn,
    ExecutionTime,
    Success
FROM __MigrationHistory
ORDER BY Version;
```

### Xem migrations lỗi

```sql
SELECT * FROM __MigrationHistory WHERE Success = 0;
```

### Xem migrations chậm

```sql
SELECT TOP 10
    Version,
    Description,
    ExecutionTime
FROM __MigrationHistory
WHERE Success = 1
ORDER BY ExecutionTime DESC;
```

## 🔧 Troubleshooting

### Migration không chạy?

**Check list:**
1. ✅ Tên file đúng format `V{version}__{description}.sql`?
2. ✅ File nằm trong thư mục `migrations/`?
3. ✅ Version chưa tồn tại trong `__MigrationHistory`?
4. ✅ Xem log application khi start?

### Migration bị lỗi?

```sql
-- 1. Xem error message
SELECT * FROM __MigrationHistory WHERE Success = 0;

-- 2. Fix migration file

-- 3. Xóa record lỗi
DELETE FROM __MigrationHistory WHERE Version = '00X';

-- 4. Restart application để chạy lại
```

### Cần rollback?

```sql
-- Option 1: Tạo migration mới để revert
-- V005__Revert_Something.sql
DROP TABLE IF EXISTS SomeTable;

-- Option 2: Manual rollback (emergency only)
-- Rollback changes manually
DELETE FROM __MigrationHistory WHERE Version = '004';
-- Restart app
```

## 📚 Tài liệu

| File | Mô tả |
|------|-------|
| [migrations/README.md](migrations/README.md) | Hướng dẫn chi tiết migration |
| [migrations/CHANGELOG.md](migrations/CHANGELOG.md) | Lịch sử thay đổi database |
| [docs/DATABASE_MIGRATION_SYSTEM.md](docs/DATABASE_MIGRATION_SYSTEM.md) | Tài liệu kỹ thuật đầy đủ |
| [sqlscript.sql](sqlscript.sql) | Base schema (reference only) |

## 🎉 Lợi ích

1. ✅ **Version control**: Mọi thay đổi DB đều được track
2. ✅ **Tự động**: Không cần chạy SQL manual
3. ✅ **An toàn**: Idempotent, có transaction
4. ✅ **Audit trail**: Log đầy đủ trong __MigrationHistory
5. ✅ **Team collaboration**: Dễ merge code, không conflict
6. ✅ **Rollback**: Có thể revert thay đổi
7. ✅ **CI/CD ready**: Tự động chạy khi deploy

## 🚀 Next Steps

1. **Chạy application** để apply 3 migrations hiện có
2. **Kiểm tra** bảng `__MigrationHistory`
3. **Thử tạo** migration mới (ví dụ V004)
4. **Đọc** [migrations/README.md](migrations/README.md) để hiểu rõ hơn
5. **Share** với team về hệ thống mới

---

Từ giờ trở đi, **MỌI thay đổi database phải thông qua migration files**! 🎯

