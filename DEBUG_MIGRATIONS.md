# 🔍 Debug Migration Issues

## Vấn đề: Bảng __MigrationHistory trống

### Các nguyên nhân có thể:

#### 1. Thư mục migrations không đúng vị trí

**Kiểm tra:**
```bash
# Xem cấu trúc thư mục
dir C:\Users\Nghia\Desktop\temp\blockchainHC

# Thư mục migrations phải nằm ở project root
# Cấu trúc đúng:
blockchainHC/
├── migrations/        <--- PHẢI Ở ĐÂY
│   ├── V001__Initial_Schema.sql
│   ├── V002__Add_Employee_Extended_Fields.sql
│   └── V003__Update_Stored_Procedures.sql
├── crmHuman/
└── ...
```

**Nếu thư mục migrations nằm trong crmHuman/:**
```bash
# DI CHUYỂN thư mục migrations ra ngoài project root
move C:\Users\Nghia\Desktop\temp\blockchainHC\crmHuman\migrations C:\Users\Nghia\Desktop\temp\blockchainHC\migrations
```

#### 2. Console log không thấy

**Chạy lại và CHÚ Ý log:**
```bash
cd C:\Users\Nghia\Desktop\temp\blockchainHC\crmHuman
dotnet run
```

**Tìm dòng log:**
```
=== Bắt đầu chạy database migration ===
ContentRootPath: C:\Users\Nghia\Desktop\temp\blockchainHC\crmHuman
Đang tìm thư mục migration tại: C:\Users\Nghia\Desktop\temp\blockchainHC\crmHuman\migrations
Thử tìm ở parent directory: C:\Users\Nghia\Desktop\temp\blockchainHC\migrations
Tìm thấy 3 file migration trong thư mục 'migrations'  <--- TÌM DÒNG NÀY
```

**Nếu thấy:**
- `Không tìm thấy file migration nào` → Thư mục sai vị trí
- `Thư mục migration không tồn tại` → Cần di chuyển thư mục

#### 3. Connection string sai

**Kiểm tra appsettings.json:**
```json
{
  "ConnectionStrings": {
    "stringConnect7": "Server=...;Database=humandev;..."
  }
}
```

**Test connection:**
```sql
-- Trong SQL Server Management Studio
-- Thử kết nối với connection string trong appsettings.json
```

#### 4. Quyền database không đủ

**Kiểm tra quyền:**
```sql
-- User cần các quyền sau:
-- CREATE TABLE
-- ALTER TABLE
-- CREATE PROCEDURE
-- ALTER PROCEDURE

-- Kiểm tra user hiện tại
SELECT USER_NAME();

-- Kiểm tra quyền
SELECT * FROM sys.database_permissions 
WHERE grantee_principal_id = USER_ID();
```

### Giải pháp nhanh:

#### Option 1: Copy file migrations vào đúng chỗ

```bash
# 1. Tạo thư mục migrations ở project root (nếu chưa có)
mkdir C:\Users\Nghia\Desktop\temp\blockchainHC\migrations

# 2. Copy các file migration
copy migrations\*.sql C:\Users\Nghia\Desktop\temp\blockchainHC\migrations\

# 3. Verify
dir C:\Users\Nghia\Desktop\temp\blockchainHC\migrations
```

#### Option 2: Chạy migrations manual

Nếu automatic không work, chạy manual:

```bash
# Trong SQL Server Management Studio hoặc sqlcmd
sqlcmd -S your_server -d humandev -U your_user -P your_password -i "C:\Users\Nghia\Desktop\temp\blockchainHC\migrations\V001__Initial_Schema.sql"
sqlcmd -S your_server -d humandev -U your_user -P your_password -i "C:\Users\Nghia\Desktop\temp\blockchainHC\migrations\V002__Add_Employee_Extended_Fields.sql"
sqlcmd -S your_server -d humandev -U your_user -P your_password -i "C:\Users\Nghia\Desktop\temp\blockchainHC\migrations\V003__Update_Stored_Procedures.sql"
```

Sau đó thêm record vào __MigrationHistory:
```sql
INSERT INTO __MigrationHistory (Version, Description, FileName, Success)
VALUES 
('001', 'Initial Schema', 'V001__Initial_Schema.sql', 1),
('002', 'Add Employee Extended Fields', 'V002__Add_Employee_Extended_Fields.sql', 1),
('003', 'Update Stored Procedures', 'V003__Update_Stored_Procedures.sql', 1);
```

### Checklist Debug:

- [ ] Thư mục migrations ở đúng project root?
- [ ] Console log có hiển thị "Tìm thấy X file migration"?
- [ ] Connection string đúng và connect được database?
- [ ] User có đủ quyền CREATE TABLE, ALTER TABLE?
- [ ] Bảng __MigrationHistory đã được tạo?
- [ ] Có lỗi gì trong console log không?

### Test manual:

```sql
-- 1. Kiểm tra bảng __MigrationHistory tồn tại
SELECT * FROM sys.tables WHERE name = '__MigrationHistory';

-- 2. Nếu có, xem nội dung
SELECT * FROM __MigrationHistory;

-- 3. Nếu trống, xem có lỗi gì khi insert
INSERT INTO __MigrationHistory (Version, Description, FileName, Success)
VALUES ('001', 'Test', 'test.sql', 1);

-- 4. Kiểm tra các cột mới đã có chưa
SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Employees' 
AND COLUMN_NAME IN ('Gender', 'PlaceOfBirth', 'Religion', 'PersonalEmail', 'BeneficiaryName');
```

### Nếu vẫn không work:

Cho tôi biết:
1. Console log đầy đủ khi chạy `dotnet run`
2. Kết quả query: `SELECT * FROM sys.tables WHERE name = '__MigrationHistory'`
3. Cấu trúc thư mục: `dir C:\Users\Nghia\Desktop\temp\blockchainHC`
4. Có file nào trong migrations/: `dir migrations`

