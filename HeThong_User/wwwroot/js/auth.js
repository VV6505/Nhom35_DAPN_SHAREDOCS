// auth.js - Quản lý authentication và localStorage

const AuthManager = {
    // Lưu thông tin đăng nhập
    saveUserInfo: function(userInfo) {
        try {
            localStorage.setItem('userInfo', JSON.stringify(userInfo));
            localStorage.setItem('isLoggedIn', 'true');
            localStorage.setItem('loginTime', new Date().toISOString());
            console.log('✅ Đã lưu thông tin đăng nhập vào localStorage');
            return true;
        } catch (error) {
            console.error('❌ Lỗi khi lưu vào localStorage:', error);
            return false;
        }
    },

    // Lấy thông tin người dùng
    getUserInfo: function() {
        try {
            const userInfoStr = localStorage.getItem('userInfo');
            if (userInfoStr) {
                return JSON.parse(userInfoStr);
            }
            return null;
        } catch (error) {
            console.error('❌ Lỗi khi đọc từ localStorage:', error);
            return null;
        }
    },

    // Kiểm tra đã đăng nhập chưa
    isLoggedIn: function() {
        return localStorage.getItem('isLoggedIn') === 'true' && this.getUserInfo() !== null;
    },

    // Xóa thông tin đăng nhập (logout)
    clearUserInfo: function() {
        try {
            localStorage.removeItem('userInfo');
            localStorage.removeItem('isLoggedIn');
            localStorage.removeItem('loginTime');
            console.log('✅ Đã xóa thông tin đăng nhập khỏi localStorage');
            return true;
        } catch (error) {
            console.error('❌ Lỗi khi xóa localStorage:', error);
            return false;
        }
    },

    // Lấy thời gian đăng nhập
    getLoginTime: function() {
        const loginTime = localStorage.getItem('loginTime');
        return loginTime ? new Date(loginTime) : null;
    },

    // Kiểm tra session có hết hạn không (mặc định 2 giờ)
    isSessionExpired: function(maxHours = 2) {
        const loginTime = this.getLoginTime();
        if (!loginTime) return true;
        
        const now = new Date();
        const diffHours = (now - loginTime) / (1000 * 60 * 60);
        return diffHours > maxHours;
    },

    // Hiển thị thông tin người dùng trong console
    displayUserInfo: function() {
        const userInfo = this.getUserInfo();
        if (userInfo) {
            console.log('👤 Thông tin người dùng:');
            console.log('- Tên tài khoản:', userInfo.TenTaiKhoan);
            console.log('- Tên người dùng:', userInfo.TenNguoiDung);
            console.log('- Loại người dùng:', userInfo.LoaiNguoiDung);
            console.log('- Email:', userInfo.Email);
            console.log('- Điểm tích lũy:', userInfo.DiemTichLuy);
            console.log('- Thời gian đăng nhập:', this.getLoginTime());
        } else {
            console.log('❌ Chưa đăng nhập');
        }
    },

    // Cập nhật một phần thông tin người dùng
    updateUserInfo: function(updates) {
        const userInfo = this.getUserInfo();
        if (userInfo) {
            const updatedInfo = { ...userInfo, ...updates };
            return this.saveUserInfo(updatedInfo);
        }
        return false;
    },

    // Lấy tên hiển thị
    getDisplayName: function() {
        const userInfo = this.getUserInfo();
        return userInfo ? userInfo.TenNguoiDung : 'Khách';
    },

    // Lấy vai trò
    getRole: function() {
        const userInfo = this.getUserInfo();
        return userInfo ? userInfo.LoaiNguoiDung : null;
    },

    // Kiểm tra có phải sinh viên không
    isSinhVien: function() {
        return this.getRole() === 'SinhVien';
    },

    // Kiểm tra có phải giảng viên không
    isGiangVien: function() {
        return this.getRole() === 'GiangVien';
    },

    // Kiểm tra có phải admin không
    isAdmin: function() {
        const userInfo = this.getUserInfo();
        return userInfo && userInfo.TenVaiTro && userInfo.TenVaiTro.toLowerCase().includes('admin');
    }
};

// Export để sử dụng ở các file khác
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AuthManager;
}

// Hiển thị thông tin khi load trang (chỉ trong development)
if (window.location.hostname === 'localhost') {
    console.log('🔐 Auth Manager đã được load');
    if (AuthManager.isLoggedIn()) {
        AuthManager.displayUserInfo();
    }
}
