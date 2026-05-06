# 📦 Triển khai LocalStorage cho Authentication

## Tổng quan thay đổi

Đã triển khai hệ thống lưu trữ thông tin đăng nhập vào **localStorage** của trình duyệt sau khi đăng nhập thành công.

## Files đã thay đổi/tạo mới

### 1. **Controllers/AuthController.cs** ✏️
**Thay đổi:**
- Cập nhật action `Login` để trả về JSON khi là AJAX request
- Tạo object `userInfo` chứa đầy đủ thông tin người dùng
- Hỗ trợ cả Sinh viên và Giảng viên
- Thêm `TempData["UserInfo"]` để truyền dữ liệu
- Cập nhật action `Logout` để signal xóa localStorage

**Code mới:**
```csharp
// Kiểm tra nếu là AJAX request thì trả về JSON
if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
{
    return Json(new { success = true, message = "Đăng nhập thành công!", userInfo });
}
```

### 2. **Views/Auth/Login.cshtml** ✏️
**Thay đổi:**
- Thêm JavaScript xử lý form submit với AJAX
- Lưu thông tin vào localStorage sau khi đăng nhập thành công
- Kiểm tra và xóa localStorage khi logout
- Hiển thị loading state khi đăng nhập

**Dữ liệu lưu:**
```javascript
localStorage.setItem('userInfo', JSON.stringify(result.userInfo));
localStorage.setItem('isLoggedIn', 'true');
localStorage.setItem('loginTime', new Date().toISOString());
```

### 3. **wwwroot/js/auth.js** ✨ MỚI
**Chức năng:**
- Object `AuthManager` quản lý localStorage
- Các phương thức tiện ích:
  - `saveUserInfo()` - Lưu thông tin
  - `getUserInfo()` - Lấy thông tin
  - `isLoggedIn()` - Kiểm tra đăng nhập
  - `clearUserInfo()` - Xóa thông tin
  - `isSessionExpired()` - Kiểm tra timeout
  - `getDisplayName()` - Lấy tên hiển thị
  - `getRole()` - Lấy vai trò
  - `isSinhVien()`, `isGiangVien()`, `isAdmin()` - Kiểm tra vai trò

### 4. **Views/Shared/_Layout.cshtml** ✏️
**Thay đổi:**
- Include file `auth.js`
- Cập nhật thông tin người dùng từ localStorage khi load trang
- Tự động cập nhật:
  - Tên người dùng
  - Avatar (chữ cái đầu)
  - Điểm tích lũy
- Xử lý đăng xuất với xác nhận

**Code mới:**
```javascript
document.addEventListener('DOMContentLoaded', function() {
    if (AuthManager.isLoggedIn()) {
        const userInfo = AuthManager.getUserInfo();
        // Cập nhật UI...
    }
});
```

### 5. **wwwroot/test-localstorage.html** ✨ MỚI
**Chức năng:**
- Trang test và debug localStorage
- Hiển thị thông tin người dùng
- Hiển thị thông tin session
- Các thao tác: refresh, test login, clear, export
- Console log real-time
- Auto refresh mỗi 5 giây

**Truy cập:** `http://localhost:5292/test-localstorage.html`

### 6. **LOCALSTORAGE_GUIDE.md** ✨ MỚI
Tài liệu hướng dẫn chi tiết cách sử dụng localStorage và AuthManager.

## Cấu trúc dữ liệu localStorage

### userInfo (JSON Object)
```json
{
  "MaTaiKhoan": "TK009",
  "TenTaiKhoan": "sv_mai",
  "MaVaiTro": "VT003",
  "TenVaiTro": "Sinh viên",
  "LoaiNguoiDung": "SinhVien",
  "MaNguoiDung": "SV006",
  "TenNguoiDung": "Hoàng Thanh Mai",
  "Email": "mai@student.ute.edu.vn",
  "DiemTichLuy": 340
}
```

### isLoggedIn (String)
```
"true"
```

### loginTime (ISO DateTime String)
```
"2026-05-06T09:30:15.123Z"
```

## Flow đăng nhập mới

```
1. User nhập username/password
   ↓
2. Form submit với AJAX (X-Requested-With: XMLHttpRequest)
   ↓
3. Server validate và trả về JSON:
   {
     success: true,
     message: "Đăng nhập thành công!",
     userInfo: { ... }
   }
   ↓
4. Client lưu vào localStorage:
   - userInfo
   - isLoggedIn
   - loginTime
   ↓
5. Redirect về trang chủ
   ↓
6. Layout load và cập nhật UI từ localStorage
```

## Flow đăng xuất

```
1. User click "Đăng xuất"
   ↓
2. Confirm dialog
   ↓
3. AuthManager.clearUserInfo() - Xóa localStorage
   ↓
4. Redirect về /Auth/Logout
   ↓
5. Server clear session
   ↓
6. Redirect về /Auth/Login với TempData["ClearLocalStorage"]
   ↓
7. Login page xóa localStorage (double check)
```

## Tính năng chính

### ✅ Đã triển khai

1. **Lưu thông tin đăng nhập** - Tự động sau khi login thành công
2. **Cập nhật UI tự động** - Tên, avatar, điểm từ localStorage
3. **Kiểm tra session timeout** - Mặc định 2 giờ
4. **Xóa khi đăng xuất** - Tự động clear localStorage
5. **AuthManager utility** - Object tiện ích cho JavaScript
6. **Test page** - Trang debug và test localStorage
7. **Hỗ trợ AJAX** - Login không reload trang
8. **Loading state** - Hiển thị trạng thái đang xử lý

### 🔒 Bảo mật

1. ✅ Không lưu mật khẩu
2. ✅ Chỉ lưu thông tin cơ bản
3. ✅ Session timeout check
4. ✅ Auto clear khi logout
5. ✅ Server vẫn validate session
6. ✅ HTTPS recommended (production)

## Cách sử dụng

### Trong JavaScript

```javascript
// Kiểm tra đăng nhập
if (AuthManager.isLoggedIn()) {
    console.log('Đã đăng nhập');
}

// Lấy thông tin
const userInfo = AuthManager.getUserInfo();
console.log(userInfo.TenNguoiDung);

// Kiểm tra vai trò
if (AuthManager.isSinhVien()) {
    // Hiển thị menu sinh viên
}

// Kiểm tra timeout
if (AuthManager.isSessionExpired()) {
    alert('Phiên đã hết hạn!');
    window.location.href = '/Auth/Login';
}
```

### Trong View (Razor)

```html
<script src="~/js/auth.js"></script>
<script>
document.addEventListener('DOMContentLoaded', function() {
    if (AuthManager.isLoggedIn()) {
        const userInfo = AuthManager.getUserInfo();
        document.getElementById('userName').textContent = userInfo.TenNguoiDung;
    }
});
</script>
```

## Testing

### 1. Test đăng nhập
```
1. Mở http://localhost:5292/Auth/Login
2. Đăng nhập với: sv_mai / 123456
3. Mở DevTools (F12) > Console
4. Gõ: AuthManager.displayUserInfo()
5. Kiểm tra output
```

### 2. Test localStorage
```
1. Mở http://localhost:5292/test-localstorage.html
2. Xem thông tin hiển thị
3. Test các chức năng:
   - Làm mới dữ liệu
   - Test đăng nhập
   - Xóa localStorage
   - Export JSON
```

### 3. Test session timeout
```javascript
// Trong console
AuthManager.isSessionExpired(); // false nếu mới đăng nhập
AuthManager.isSessionExpired(0.01); // true (timeout 0.01 giờ = 36 giây)
```

### 4. Test đăng xuất
```
1. Đăng nhập
2. Kiểm tra localStorage có dữ liệu
3. Click "Đăng xuất"
4. Kiểm tra localStorage đã bị xóa
```

## Troubleshooting

### Vấn đề: localStorage không lưu

**Giải pháp:**
1. Kiểm tra trình duyệt có bật localStorage
2. Không dùng Private/Incognito mode
3. Xóa cache và thử lại
4. Kiểm tra console có lỗi

### Vấn đề: Thông tin không cập nhật

**Giải pháp:**
```javascript
// Force clear và reload
AuthManager.clearUserInfo();
window.location.reload();
```

### Vấn đề: Session timeout không hoạt động

**Giải pháp:**
```javascript
// Kiểm tra thời gian
const loginTime = AuthManager.getLoginTime();
console.log('Login time:', loginTime);
console.log('Is expired:', AuthManager.isSessionExpired());
```

## Lưu ý quan trọng

⚠️ **QUAN TRỌNG:**

1. **localStorage có thể bị chỉnh sửa** - Luôn validate ở server
2. **Không lưu thông tin nhạy cảm** - Chỉ lưu thông tin cơ bản
3. **Session timeout** - Tự động đăng xuất sau 2 giờ
4. **HTTPS trong production** - Bảo vệ dữ liệu truyền tải
5. **Backup với Session** - Server vẫn dùng Session làm chính

## Tài liệu tham khảo

- `LOCALSTORAGE_GUIDE.md` - Hướng dẫn chi tiết
- `wwwroot/js/auth.js` - Source code AuthManager
- `wwwroot/test-localstorage.html` - Test page

## Changelog

### Version 1.0 (2026-05-06)
- ✅ Triển khai localStorage cho authentication
- ✅ Tạo AuthManager utility
- ✅ Cập nhật UI tự động từ localStorage
- ✅ Thêm session timeout check
- ✅ Tạo test page
- ✅ Viết tài liệu hướng dẫn

## Tác giả

Hệ thống chia sẻ tài liệu UTE - 2026
