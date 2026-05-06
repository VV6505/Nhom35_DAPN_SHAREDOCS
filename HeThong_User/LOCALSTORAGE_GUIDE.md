# 📦 Hướng dẫn sử dụng LocalStorage

## Tổng quan

Sau khi đăng nhập thành công, hệ thống sẽ tự động lưu thông tin người dùng vào **localStorage** của trình duyệt. Điều này giúp:
- Duy trì trạng thái đăng nhập khi refresh trang
- Truy cập nhanh thông tin người dùng từ JavaScript
- Giảm tải cho server (không cần query session mỗi lần)

## Dữ liệu được lưu

### 1. `userInfo` (Object JSON)
Chứa toàn bộ thông tin người dùng:

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

### 2. `isLoggedIn` (Boolean string)
- Giá trị: `"true"` hoặc không tồn tại
- Dùng để kiểm tra nhanh trạng thái đăng nhập

### 3. `loginTime` (ISO DateTime string)
- Ví dụ: `"2026-05-06T09:30:15.123Z"`
- Dùng để kiểm tra session timeout

## Sử dụng AuthManager

File `wwwroot/js/auth.js` cung cấp object `AuthManager` với các phương thức tiện ích:

### Kiểm tra đăng nhập

```javascript
// Kiểm tra đã đăng nhập chưa
if (AuthManager.isLoggedIn()) {
    console.log('Đã đăng nhập');
}

// Lấy thông tin người dùng
const userInfo = AuthManager.getUserInfo();
console.log(userInfo.TenNguoiDung); // "Hoàng Thanh Mai"

// Lấy tên hiển thị
const displayName = AuthManager.getDisplayName(); // "Hoàng Thanh Mai"

// Lấy vai trò
const role = AuthManager.getRole(); // "SinhVien"
```

### Kiểm tra vai trò

```javascript
// Kiểm tra có phải sinh viên không
if (AuthManager.isSinhVien()) {
    console.log('Đây là sinh viên');
}

// Kiểm tra có phải giảng viên không
if (AuthManager.isGiangVien()) {
    console.log('Đây là giảng viên');
}

// Kiểm tra có phải admin không
if (AuthManager.isAdmin()) {
    console.log('Đây là admin');
}
```

### Cập nhật thông tin

```javascript
// Cập nhật điểm tích lũy
AuthManager.updateUserInfo({ DiemTichLuy: 500 });

// Cập nhật email
AuthManager.updateUserInfo({ Email: 'newemail@ute.edu.vn' });
```

### Kiểm tra session timeout

```javascript
// Kiểm tra session có hết hạn không (mặc định 2 giờ)
if (AuthManager.isSessionExpired()) {
    alert('Phiên đăng nhập đã hết hạn!');
    window.location.href = '/Auth/Login';
}

// Kiểm tra với thời gian tùy chỉnh (4 giờ)
if (AuthManager.isSessionExpired(4)) {
    console.log('Session hết hạn sau 4 giờ');
}
```

### Đăng xuất

```javascript
// Xóa toàn bộ thông tin đăng nhập
AuthManager.clearUserInfo();

// Redirect về trang login
window.location.href = '/Auth/Login';
```

### Debug

```javascript
// Hiển thị thông tin người dùng trong console
AuthManager.displayUserInfo();

// Output:
// 👤 Thông tin người dùng:
// - Tên tài khoản: sv_mai
// - Tên người dùng: Hoàng Thanh Mai
// - Loại người dùng: SinhVien
// - Email: mai@student.ute.edu.vn
// - Điểm tích lũy: 340
// - Thời gian đăng nhập: Wed May 06 2026 16:30:15 GMT+0700
```

## Tích hợp trong View

### Hiển thị thông tin người dùng

```html
<div id="userProfile"></div>

<script>
document.addEventListener('DOMContentLoaded', function() {
    if (AuthManager.isLoggedIn()) {
        const userInfo = AuthManager.getUserInfo();
        document.getElementById('userProfile').innerHTML = `
            <h3>Xin chào, ${userInfo.TenNguoiDung}!</h3>
            <p>Email: ${userInfo.Email}</p>
            <p>Điểm tích lũy: ${userInfo.DiemTichLuy}</p>
        `;
    }
});
</script>
```

### Bảo vệ trang yêu cầu đăng nhập

```javascript
document.addEventListener('DOMContentLoaded', function() {
    // Kiểm tra đăng nhập
    if (!AuthManager.isLoggedIn()) {
        alert('Vui lòng đăng nhập để truy cập trang này!');
        window.location.href = '/Auth/Login';
        return;
    }
    
    // Kiểm tra session timeout
    if (AuthManager.isSessionExpired()) {
        alert('Phiên đăng nhập đã hết hạn!');
        AuthManager.clearUserInfo();
        window.location.href = '/Auth/Login';
        return;
    }
    
    // Trang được load bình thường
    console.log('Người dùng đã đăng nhập:', AuthManager.getDisplayName());
});
```

### Hiển thị nội dung theo vai trò

```javascript
if (AuthManager.isSinhVien()) {
    document.getElementById('studentSection').style.display = 'block';
} else if (AuthManager.isGiangVien()) {
    document.getElementById('teacherSection').style.display = 'block';
} else if (AuthManager.isAdmin()) {
    document.getElementById('adminSection').style.display = 'block';
}
```

## Lưu ý bảo mật

⚠️ **QUAN TRỌNG:**

1. **Không lưu mật khẩu vào localStorage** - Hệ thống chỉ lưu thông tin cơ bản, không bao gồm mật khẩu
2. **Không lưu thông tin nhạy cảm** - Chỉ lưu thông tin cần thiết cho UI
3. **Luôn validate ở server** - localStorage có thể bị chỉnh sửa, luôn kiểm tra session ở server
4. **Xóa localStorage khi đăng xuất** - Đảm bảo không còn thông tin sau khi logout
5. **Kiểm tra session timeout** - Tự động đăng xuất sau thời gian nhất định

## Test trong Console

Mở Developer Tools (F12) và thử các lệnh sau:

```javascript
// Xem thông tin đăng nhập
AuthManager.displayUserInfo();

// Xem raw data
console.log(localStorage.getItem('userInfo'));

// Kiểm tra trạng thái
console.log('Đã đăng nhập:', AuthManager.isLoggedIn());
console.log('Vai trò:', AuthManager.getRole());
console.log('Tên:', AuthManager.getDisplayName());

// Xóa localStorage (test logout)
AuthManager.clearUserInfo();
```

## Tích hợp với AJAX

Khi gọi API, có thể gửi kèm thông tin từ localStorage:

```javascript
async function callAPI() {
    const userInfo = AuthManager.getUserInfo();
    
    const response = await fetch('/api/documents', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-User-Id': userInfo.MaNguoiDung
        },
        body: JSON.stringify({
            userId: userInfo.MaNguoiDung,
            userName: userInfo.TenNguoiDung
        })
    });
    
    return await response.json();
}
```

## Troubleshooting

### LocalStorage không hoạt động?

1. Kiểm tra trình duyệt có bật localStorage không
2. Kiểm tra chế độ Private/Incognito (có thể bị giới hạn)
3. Xóa cache và thử lại
4. Kiểm tra console có lỗi không

### Thông tin không cập nhật?

```javascript
// Force reload từ server
AuthManager.clearUserInfo();
window.location.reload();
```

### Session timeout không hoạt động?

```javascript
// Kiểm tra thời gian đăng nhập
const loginTime = AuthManager.getLoginTime();
console.log('Đăng nhập lúc:', loginTime);

const now = new Date();
const diffHours = (now - loginTime) / (1000 * 60 * 60);
console.log('Đã đăng nhập được:', diffHours, 'giờ');
```

## Tài liệu tham khảo

- [MDN - Web Storage API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API)
- [localStorage vs sessionStorage](https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API/Using_the_Web_Storage_API)
