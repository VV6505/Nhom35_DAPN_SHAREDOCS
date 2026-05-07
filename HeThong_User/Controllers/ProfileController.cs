using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_User.Controllers
{
    public class ProfileController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(HeThongChiaSeTaiLieu_V1 context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Profile/Index
        public IActionResult Index()
        {
            // Kiểm tra session
            var maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan");
            if (string.IsNullOrEmpty(maTaiKhoan))
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // GET: Profile/GetUserInfo - API endpoint để lấy thông tin từ server
        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan");
                if (string.IsNullOrEmpty(maTaiKhoan))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var taiKhoan = await _context.TaiKhoans
                    .Include(tk => tk.MaVaiTroNavigation)
                    .Include(tk => tk.MaSvNavigation)
                    .Include(tk => tk.MaGvNavigation)
                    .FirstOrDefaultAsync(tk => tk.MaTk == maTaiKhoan);

                if (taiKhoan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                object userInfo = null;

                if (taiKhoan.MaSvNavigation != null)
                {
                    var sinhVien = taiKhoan.MaSvNavigation;
                    userInfo = new
                    {
                        maTaiKhoan = taiKhoan.MaTk,
                        tenTaiKhoan = taiKhoan.TenTk,
                        maVaiTro = taiKhoan.MaVaiTro,
                        tenVaiTro = taiKhoan.MaVaiTroNavigation?.TenVaiTro,
                        loaiNguoiDung = "SinhVien",
                        maNguoiDung = sinhVien.MaSv,
                        tenNguoiDung = sinhVien.TenSv,
                        email = sinhVien.Email,
                        ngaySinh = sinhVien.NgaySinh?.ToString("dd/MM/yyyy"),
                        gioiTinh = sinhVien.GioiTinh,
                        diemTichLuy = sinhVien.DiemTichLuy ?? 0,
                        maLop = sinhVien.MaLop,
                        trangThai = sinhVien.TrangThaiSv
                    };
                }
                else if (taiKhoan.MaGvNavigation != null)
                {
                    var giangVien = taiKhoan.MaGvNavigation;
                    userInfo = new
                    {
                        maTaiKhoan = taiKhoan.MaTk,
                        tenTaiKhoan = taiKhoan.TenTk,
                        maVaiTro = taiKhoan.MaVaiTro,
                        tenVaiTro = taiKhoan.MaVaiTroNavigation?.TenVaiTro,
                        loaiNguoiDung = "GiangVien",
                        maNguoiDung = giangVien.MaGv,
                        tenNguoiDung = giangVien.TenGv,
                        email = giangVien.Email,
                        ngaySinh = giangVien.NgaySinh?.ToString("dd/MM/yyyy"),
                        gioiTinh = giangVien.GioiTinh,
                        diemTichLuy = 0,
                        hocVi = giangVien.HocVi,
                        sdt = giangVien.Sdt,
                        maKhoa = giangVien.MaKhoa
                    };
                }

                return Json(new { success = true, data = userInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin người dùng");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Profile/GetStats - Lấy thống kê tài liệu
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var loaiNguoiDung = HttpContext.Session.GetString("LoaiNguoiDung");
                var maNguoiDung = loaiNguoiDung == "SinhVien" 
                    ? HttpContext.Session.GetString("MaSinhVien")
                    : HttpContext.Session.GetString("MaGiangVien");

                if (string.IsNullOrEmpty(maNguoiDung))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                // Đếm số tài liệu đã đăng
                var soTaiLieuDaDang = await _context.TaiLieus
                    .Where(tl => tl.MaNguoiDang == maNguoiDung)
                    .CountAsync();

                // Đếm số lượt tải xuống (tổng lượt tải của tất cả tài liệu)
                var tongLuotTai = await _context.TaiLieus
                    .Where(tl => tl.MaNguoiDang == maNguoiDung)
                    .SumAsync(tl => tl.LuotTai ?? 0);

                // Đếm số đánh giá
                var soDanhGia = await _context.DanhGia
                    .Where(dg => _context.TaiLieus
                        .Where(tl => tl.MaNguoiDang == maNguoiDung)
                        .Select(tl => tl.MaTaiLieu)
                        .Contains(dg.MaTl ?? ""))
                    .CountAsync();

                return Json(new 
                { 
                    success = true,
                    soTaiLieuDaDang = soTaiLieuDaDang,
                    tongLuotTai = tongLuotTai,
                    soDanhGia = soDanhGia
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // POST: Profile/UpdateProfile - Cập nhật thông tin
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan");
                if (string.IsNullOrEmpty(maTaiKhoan))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var taiKhoan = await _context.TaiKhoans
                    .Include(tk => tk.MaSvNavigation)
                    .Include(tk => tk.MaGvNavigation)
                    .FirstOrDefaultAsync(tk => tk.MaTk == maTaiKhoan);

                if (taiKhoan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (taiKhoan.MaSvNavigation != null)
                {
                    var sinhVien = taiKhoan.MaSvNavigation;
                    
                    if (!string.IsNullOrEmpty(request.Email))
                        sinhVien.Email = request.Email;
                    
                    if (!string.IsNullOrEmpty(request.GioiTinh))
                        sinhVien.GioiTinh = request.GioiTinh;
                    
                    if (request.NgaySinh.HasValue)
                        sinhVien.NgaySinh = request.NgaySinh.Value;
                }
                else if (taiKhoan.MaGvNavigation != null)
                {
                    var giangVien = taiKhoan.MaGvNavigation;
                    
                    if (!string.IsNullOrEmpty(request.Email))
                        giangVien.Email = request.Email;
                    
                    if (!string.IsNullOrEmpty(request.GioiTinh))
                        giangVien.GioiTinh = request.GioiTinh;
                    
                    if (request.NgaySinh.HasValue)
                        giangVien.NgaySinh = request.NgaySinh.Value;
                    
                    if (!string.IsNullOrEmpty(request.Sdt))
                        giangVien.Sdt = request.Sdt;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật thông tin");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật" });
            }
        }

        // POST: Profile/ChangePassword - Đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan");
                if (string.IsNullOrEmpty(maTaiKhoan))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var taiKhoan = await _context.TaiKhoans.FindAsync(maTaiKhoan);
                if (taiKhoan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                // Kiểm tra mật khẩu cũ
                if (taiKhoan.MatKhau?.Trim() != request.OldPassword?.Trim())
                {
                    return Json(new { success = false, message = "Mật khẩu cũ không đúng!" });
                }

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự!" });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp!" });
                }

                // Cập nhật mật khẩu
                taiKhoan.MatKhau = request.NewPassword;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đổi mật khẩu" });
            }
        }
    }

    // Request models
    public class UpdateProfileRequest
    {
        public string? Email { get; set; }
        public string? GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? Sdt { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
