using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThong_User.Models;

namespace HeThong_Admin.Controllers
{
    public class AuthController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public AuthController(HeThongChiaSeTaiLieu_V1 context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string? username, string? password, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return View();
            }

            try
            {
                // Truy vấn tài khoản dựa trên MaTK (ID) cho đồng bộ với bên User
                var taiKhoan = await _context.TaiKhoans
                    .Include(tk => tk.MaVaiTroNavigation)
                    .FirstOrDefaultAsync(tk => username != null && tk.MaTk.Trim() == username.Trim());

                if (taiKhoan == null)
                {
                    ViewBag.Error = "Tài khoản không tồn tại trong hệ thống.";
                    return View();
                }

                // Kiểm tra mật khẩu
                if (taiKhoan.MatKhau != password)
                {
                    ViewBag.Error = "Mật khẩu không chính xác.";
                    return View();
                }

                // Kiểm tra vai trò: Chỉ cho phép VT001 (Admin) và VT004 (Cán bộ khoa)
                if (taiKhoan.MaVaiTro?.Trim() != "VT001" && taiKhoan.MaVaiTro?.Trim() != "VT004")
                {
                    ViewBag.Error = "Tài khoản của bạn không có quyền truy cập trang quản trị.";
                    return View();
                }

                // Kiểm tra trạng thái tài khoản (0 = Khóa)
                if (taiKhoan.TrangThai == 0)
                {
                    ViewBag.Error = "Tài khoản bị tạm khóa. Vui lòng liên hệ Admin để biết thêm thông tin!";
                    return View();
                }

                // Lưu thông tin vào Session
                HttpContext.Session.SetString("AdminId", taiKhoan.MaTk);
                HttpContext.Session.SetString("AdminName", taiKhoan.TenTk ?? "Admin");
                HttpContext.Session.SetString("AdminRole", taiKhoan.MaVaiTro?.Trim() ?? "");
                HttpContext.Session.SetString("RoleName", taiKhoan.MaVaiTroNavigation?.TenVaiTro ?? "Quản trị");

                // Lấy thêm thông tin chi tiết nếu là Cán bộ khoa (Giảng viên)
                if (taiKhoan.MaVaiTro?.Trim() == "VT004")
                {
                    var gv = await _context.GiangViens
                        .Include(g => g.MaKhoaNavigation)
                        .FirstOrDefaultAsync(g => g.MaGv == taiKhoan.MaTk);
                    if (gv != null)
                    {
                        HttpContext.Session.SetString("AdminEmail", gv.Email ?? "");
                        HttpContext.Session.SetString("AdminFaculty", gv.MaKhoaNavigation?.TenKhoa ?? "");
                    }
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
