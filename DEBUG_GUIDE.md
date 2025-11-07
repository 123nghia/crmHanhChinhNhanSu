# 🔧 Hướng dẫn Debug crmHuman trên Windows

## 📋 Yêu cầu

1. **.NET 7 SDK** - Tải từ [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/7.0)
2. **Visual Studio 2022** (khuyến nghị) hoặc **VS Code** với extension C#
3. **SQL Server** - Đảm bảo database đã được cấu hình trong `appsettings.json`

## 🚀 Cách 1: Sử dụng Script PowerShell (Nhanh nhất)

### Setup lần đầu:
```powershell
.\setup-debug.ps1
```

### Chạy debug:
```powershell
.\run-debug.ps1
```

## 🎯 Cách 2: Visual Studio 2022

1. Mở file `crmHuman\crmHuman.sln`
2. Nhấn **F5** hoặc chọn **Debug > Start Debugging**
3. Ứng dụng sẽ chạy tại: `https://localhost:7164`

### Cấu hình Debug trong Visual Studio:
- File `Properties/launchSettings.json` đã được cấu hình sẵn
- Profile: **crmHuman** (mặc định)
- Environment: **Development**

## 💻 Cách 3: VS Code

1. Mở thư mục project trong VS Code
2. Nhấn **F5** hoặc chọn **Run > Start Debugging**
3. Chọn profile: **.NET Core Launch (crmHuman)**

### Cấu hình đã có sẵn:
- `.vscode/launch.json` - Cấu hình debug
- `.vscode/tasks.json` - Cấu hình build tasks

## ⌨️ Cách 4: Command Line

```powershell
cd crmHuman
dotnet restore
dotnet build
dotnet run
```

## 🔍 Breakpoints và Debugging

### Đặt Breakpoint:
- Click vào số dòng bên trái trong editor
- Hoặc đặt cursor và nhấn **F9**

### Debug Commands:
- **F5** - Continue
- **F10** - Step Over
- **F11** - Step Into
- **Shift+F11** - Step Out
- **Shift+F5** - Stop Debugging

## ⚙️ Cấu hình Connection String

Chỉnh sửa file `crmHuman/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "stringConnect": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASS;..."
  }
}
```

## 🐛 Troubleshooting

### Lỗi "Path to shell executable does not exist" trong VS Code:

**Nguyên nhân**: VS Code không tìm thấy `dotnet` trong PATH hoặc shell configuration không đúng.

**Giải pháp 1**: Kiểm tra dotnet đã được cài đặt:
```powershell
.\check-dotnet.ps1
```

**Giải pháp 2**: Thêm dotnet vào PATH (nếu chưa có):
```powershell
# Mở PowerShell với quyền Administrator
[Environment]::SetEnvironmentVariable('Path', [Environment]::GetEnvironmentVariable('Path', 'User') + ';C:\Program Files\dotnet', 'User')
```

Sau đó **khởi động lại VS Code**.

**Giải pháp 3**: Cài đặt .NET 7 SDK nếu chưa có:
- Tải từ: https://dotnet.microsoft.com/download/dotnet/7.0
- Cài đặt và khởi động lại VS Code

**Giải pháp 4**: Sử dụng Visual Studio thay vì VS Code (khuyến nghị)

### Lỗi SSL Certificate:
```powershell
dotnet dev-certs https --trust
```

### Lỗi Port đã được sử dụng:
- Thay đổi port trong `Properties/launchSettings.json`
- Hoặc kill process đang dùng port:
```powershell
netstat -ano | findstr :7164
taskkill /PID <PID> /F
```

### Lỗi Build:
```powershell
dotnet clean
dotnet restore
dotnet build
```

## 📝 Notes

- File `launchSettings.json` đã được cấu hình sẵn
- Environment mặc định: **Development**
- URL mặc định: `https://localhost:7164` (HTTPS) hoặc `http://localhost:5232` (HTTP)

## 🔗 Links

- [.NET Documentation](https://docs.microsoft.com/dotnet)
- [ASP.NET Core Debugging](https://docs.microsoft.com/aspnet/core/test/debug)

