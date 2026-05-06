# 🔗 Hướng dẫn Connection String

## 📍 Các vị trí cần cập nhật

Khi thay đổi server hoặc database, bạn cần cập nhật **2 chỗ**:

### 1. `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 2. `Models/HeThongChiaSeTaiLieu_V1.cs`
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSqlServer("Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;TrustServerCertificate=True;");
```

## 🔧 Các loại Connection String

### 1. Windows Authentication (Khuyến nghị - Đang dùng)
```
Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;TrustServerCertificate=True;
```

**Ưu điểm:**
- ✅ Không cần username/password
- ✅ Bảo mật cao hơn
- ✅ Dùng tài khoản Windows hiện tại

**Khi nào dùng:**
- Khi SQL Server và ứng dụng trên cùng máy
- Khi đã setup Windows Authentication trong SQL Server

### 2. SQL Server Authentication
```
Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;User Id=sa;Password=your_password;TrustServerCertificate=True;
```

**Ưu điểm:**
- ✅ Linh hoạt hơn
- ✅ Có thể dùng từ máy khác

**Khi nào dùng:**
- Khi cần kết nối từ máy khác
- Khi không có quyền Windows Authentication

### 3. Connection String với IP
```
Server=192.168.1.100;Database=HeThongChiaSeTaiLieu_V1;User Id=sa;Password=your_password;TrustServerCertificate=True;
```

**Khi nào dùng:**
- Kết nối đến server từ xa
- Server không có tên domain

### 4. Connection String với Port
```
Server=KIEUOANH,1433;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;TrustServerCertificate=True;
```

**Khi nào dùng:**
- SQL Server chạy trên port không mặc định
- Cần chỉ định port cụ thể

### 5. Connection String với Instance Name
```
Server=KIEUOANH\SQLEXPRESS;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;TrustServerCertificate=True;
```

**Khi nào dùng:**
- Dùng SQL Server Express
- Có nhiều instance trên cùng máy

### 6. LocalDB (Development)
```
Server=(localdb)\\mssqllocaldb;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;
```

**Khi nào dùng:**
- Development trên máy local
- Không cần cài SQL Server đầy đủ

## 🔍 Cách tìm Server Name

### Cách 1: Trong SQL Server Management Studio
1. Mở SSMS
2. Xem tên server trong cửa sổ Connect to Server
3. Hoặc chạy query: `SELECT @@SERVERNAME`

### Cách 2: Trong PowerShell
```powershell
# Xem tất cả SQL Server instances
Get-Service -Name MSSQL*

# Hoặc
sqlcmd -L
```

### Cách 3: Trong Registry
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL
```

## 🎯 Các tham số quan trọng

| Tham số | Mô tả | Giá trị |
|---------|-------|---------|
| `Server` | Tên server hoặc IP | `KIEUOANH`, `localhost`, `192.168.1.100` |
| `Database` | Tên database | `HeThongChiaSeTaiLieu_V1` |
| `Trusted_Connection` | Dùng Windows Auth | `True` hoặc `False` |
| `User Id` | Username SQL | `sa`, `admin` |
| `Password` | Password SQL | `your_password` |
| `TrustServerCertificate` | Bỏ qua SSL cert | `True` (cho dev) |
| `Encrypt` | Mã hóa kết nối | `True` hoặc `False` |
| `MultipleActiveResultSets` | Cho phép nhiều query | `True` |
| `Connection Timeout` | Timeout (giây) | `30` (mặc định) |

## 🔒 Bảo mật Connection String

### ❌ KHÔNG NÊN:
```csharp
// Hardcode trong code
optionsBuilder.UseSqlServer("Server=...;Password=123456;");
```

### ✅ NÊN:
```csharp
// Đọc từ configuration
var connectionString = configuration.GetConnectionString("DefaultConnection");
optionsBuilder.UseSqlServer(connectionString);
```

### 🔐 Tốt hơn nữa:
```json
// Dùng User Secrets (Development)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Password=...;"
```

```json
// Dùng Environment Variables (Production)
export ConnectionStrings__DefaultConnection="Server=...;Password=...;"
```

## 🧪 Test Connection String

### Cách 1: Dùng sqlcmd
```bash
sqlcmd -S KIEUOANH -d HeThongChiaSeTaiLieu_V1 -E -Q "SELECT @@VERSION"
```

### Cách 2: Dùng PowerShell
```powershell
$connectionString = "Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    Write-Host "✅ Kết nối thành công!" -ForegroundColor Green
    $connection.Close()
} catch {
    Write-Host "❌ Lỗi: $_" -ForegroundColor Red
}
```

### Cách 3: Chạy ứng dụng
```bash
dotnet run
# Xem log trong console
```

## 🐛 Troubleshooting

### Lỗi: "Login failed for user"
```
Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;Trusted_Connection=True;
```
→ Kiểm tra Windows Authentication đã enable chưa

### Lỗi: "Cannot open database"
```
Server=KIEUOANH;Database=HeThongChiaSeTaiLieu_V1;...
```
→ Kiểm tra database đã tạo chưa: `CREATE DATABASE HeThongChiaSeTaiLieu_V1`

### Lỗi: "Network-related error"
```
Server=KIEUOANH;...
```
→ Kiểm tra:
1. SQL Server đang chạy
2. TCP/IP enabled
3. Firewall không chặn
4. Tên server đúng

### Lỗi: "Certificate chain was issued by an authority"
```
Server=KIEUOANH;...;TrustServerCertificate=True;
```
→ Thêm `TrustServerCertificate=True;`

## 📝 Checklist

Khi thay đổi connection string:

- [ ] Cập nhật `appsettings.json`
- [ ] Cập nhật `Models/HeThongChiaSeTaiLieu_V1.cs`
- [ ] Test kết nối: `dotnet run`
- [ ] Kiểm tra log trong console
- [ ] Thử đăng nhập vào ứng dụng

---

**Hiện tại bạn đang dùng:**
```
Server=KIEUOANH
Database=HeThongChiaSeTaiLieu_V1
Windows Authentication
```

**Đã cập nhật ở 2 chỗ! ✅**
