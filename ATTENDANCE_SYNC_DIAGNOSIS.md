# Báo Cáo: Máy Chấm Công Không Đồng Bộ - Nguyên Nhân & Giải Pháp

## Vấn Đề
Máy chấm công không đang đồng bộ lịch sử quét thẻ vào hệ thống. Service `AttendanceRealtimeSyncService` không chạy hoặc chạy nhưng không fetch dữ liệu.

## Nguyên Nhân Chính

### 1. **Cấu Hình: `UseDirectDeviceRealtime: true` NHƯNG `DeviceIp` Không Thể Kết Nối**

**Vị trí:** `appsettings.json` - Lines 19-20

```json
"UseDirectDeviceRealtime": true,
"DeviceIp": "192.168.1.34",
"DevicePort": 4370,
```

**Vấn đề:**
- Service đang được cấu hình để kết nối **trực tiếp** đến máy chấm công qua TCP/IP (IP: `192.168.1.34`, Port: `4370`)
- Nếu máy chấm công không reachable (tắt, lỗi kết nối mạng, IP sai), service sẽ **không sync** và **chỉ log warning, không báo lỗi rõ ràng**

### 2. **Luồng Kiểm Tra Cấu Hình**

Từ `AttendanceRealtimeSyncService.cs` (Lines 45-115):

```csharp
// STEP 1: Kiểm tra xem có thể chạy trên hệ điều hành hiện tại
if (!options.UseDirectSqlRealtime && !options.UseDirectDeviceRealtime && !OperatingSystem.IsWindows())
{
    _logger.LogWarning("Attendance realtime sync only runs on Windows when the source is an Access database.");
    return;  // ❌ STOP - Không phải Windows và không có Direct source
}

// STEP 2: Kiểm tra RealtimeSyncEnabled
if (!options.RealtimeSyncEnabled)
{
    LogRuntimeStateOnce("disabled", LogLevel.Information, "...");
    continue;  // ✅ OK - cấu hình là `true` (Line 24)
}

// STEP 3: Kiểm tra UseDirectDeviceRealtime - cần DeviceIp
if (options.UseDirectDeviceRealtime)
{
    if (string.IsNullOrWhiteSpace(options.DeviceIp))
    {
        LogRuntimeStateOnce("missing-device-ip", LogLevel.Warning, "...");
        continue;  // ❌ STOP - Nếu DeviceIp trống (NHƯNG hiện tại không trống!)
    }
}

// STEP 4: Kiểm tra kết nối thực tế
await LogDeviceConnectivityAsync(options);  // ⚠️ Kiểm tra nhưng không stop nếu fail
await RunSyncAsync(options, stoppingToken);
```

## Diagnosis Checklist

### ✅ Được Bật
- `RealtimeSyncEnabled: true` (Line 24)
- `UseDirectDeviceRealtime: true` (Line 19)
- `DeviceIp: "192.168.1.34"` (Line 20) - **Không trống**

### ⚠️ Cần Kiểm Tra Ngay

| Kiểm Tra | Cách Kiểm Tra | Tác Động |
|---------|--------------|---------|
| **Máy chấm công reachable?** | `ping 192.168.1.34` từ server | Nếu không reply, sync fail |
| **Port 4370 mở?** | `telnet 192.168.1.34 4370` | Nếu không mở, kết nối bị từ chối |
| **Mạng đó có còn hoạt động?** | Kiểm tra switch, firewall, VPN | Nếu mạng down, không sync |
| **Dữ liệu trong máy chấm?** | Truy cập web interface máy chấm | Nếu trống, sẽ không có gì để sync |

## Giải Pháp

### Option 1: Kích Hoạt Direct SQL Realtime (Nhanh Nhất ✅)

Nếu máy chấm công **có database SQL Server** tạm thời, dùng cách này:

```json
"AttendanceMachine": {
  "UseDirectDeviceRealtime": false,      // 🔴 Tắt
  "UseDirectSqlRealtime": true,          // 🟢 Bật
  "DirectSqlConnectionString": "Server=115.79.5.244,1433; Initial Catalog=AttendanceDirectSync;User ID=crm;Password=Vietstar@2018; ...",
  "RealtimeSyncEnabled": true
}
```

**Ưu điểm:** Không cần kết nối TCP/IP đến máy, kết nối qua SQL Server
**Hạn chế:** Máy chấm phải đang đẩy dữ liệu vào DB này

### Option 2: Kiểm Tra Và Sửa Cấu Hình Device (Chi Tiết)

```json
"AttendanceMachine": {
  "DeviceIp": "192.168.1.34",             // ✅ Kiểm tra IP máy chấm hiện tại
  "DevicePort": 4370,                     // ✅ Kiểm tra port mở trên máy chấm
  "DeviceTimeoutSeconds": 120,
  "UseDirectDeviceRealtime": true,
  "RealtimeSyncEnabled": true,
  "RealtimeSyncIntervalSeconds": 300,     // Cứ 5 phút check 1 lần
  "RealtimeSyncLookbackDays": 35          // Lấy 35 ngày gần nhất
}
```

**Các bước:**
1. Ping test: `ping 192.168.1.34` 
2. Telnet test: `telnet 192.168.1.34 4370`
3. Kiểm tra Web Interface của máy (thường port 80 hoặc 8080)
4. Xác nhận IP không thay đổi

### Option 3: Quay Lại Access Database (Có File)

Nếu máy Windows chứa file `.mdb`:

```json
"AttendanceMachine": {
  "UseAccessRealtime": true,              // Có, nhưng deprecated
  "UseDirectDeviceRealtime": false,       // Tắt Direct
  "UseDirectSqlRealtime": false,          // Tắt SQL
  "DbPath": "C:\\Users\\Administrator\\Desktop\\WiseEyeOn39.mdb",
  "RealtimeSyncEnabled": true
}
```

**Ưu điểm:** Đơn giản, chỉ cần file `.mdb` tồn tại
**Hạn chế:** Service chạy trên Linux sẽ không sync (cần Windows)

## Các Log Cần Check

Xem **Application Event Log** hoặc **file log** của service:

### ✅ Nếu Chạy Tốt
```
[INFO] Attendance realtime sync is running directly from device TCP/IP. 
       Device 192.168.1.34:4370, interval 300s, lookback 35 day(s).
[INFO] Attendance realtime sync completed initial pass: processed 5 attendance day(s), saved 150 row(s).
```

### ❌ Nếu Có Lỗi
```
[WARNING] Attendance realtime sync skipped because the sync returned 1 error(s). 
          First error: Connection refused to 192.168.1.34:4370
          
[WARNING] Device 192.168.1.34:4370 not reachable (timeout after 120s).
```

## Hành Động Khuyến Nghị

### Ngay Lập Tức (Hôm nay)
1. **SSH/RDP vào server** chạy crmHuman
2. **Chạy:** `ping 192.168.1.34` 
3. **Chạy:** `telnet 192.168.1.34 4370` (Windows cmd hoặc PowerShell)
4. **Xem logs:** Event Viewer → Application hoặc file log theo đường dẫn

### Nếu Ping Fail
- Kiểm tra máy chấm công có bật không
- Kiểm tra cáp mạng
- Kiểm tra VLAN/Firewall
- Nếu IP sai, cập nhật vào `appsettings.json`

### Nếu Telnet Fail (Connection Refused)
- Máy chấm công port 4370 chưa mở
- Cần liên hệ admin máy chấm công để:
  - Bật service TCP/IP
  - Xác nhận port 4370
  - Test kết nối từ server

### Nếu Muốn Test Ngay
```bash
# Test kết nối trực tiếp bằng PowerShell
$connection = @{
    ComputerName = '192.168.1.34'
    Port = 4370
    ErrorAction = 'SilentlyContinue'
}
$result = Test-NetConnection @connection
Write-Host "Device Reachable: $($result.PingSucceeded)"
Write-Host "TCP Port Open: $($result.TcpTestSucceeded)"
```

---

## Tóm Tắt Nguyên Nhân

| Loại | Nguyên Nhân | Trạng Thái |
|------|-----------|-----------|
| **Config** | `UseDirectDeviceRealtime: true` | ✅ Được bật |
| **IP Device** | `192.168.1.34` | ⚠️ Cần verify kết nối |
| **Service** | `AttendanceRealtimeSyncService` | ✅ Đã register |
| **Enable** | `RealtimeSyncEnabled: true` | ✅ Được bật |
| **Network** | IP → Port 4370 | ❌ **NGHI VẤN** - Cần test |

---

**Date:** 15/04/2026  
**Status:** Cần xác nhận lại kết nối mạng để máy chấm công
