using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_User.Controllers
{
    public class AuthController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(HeThongChiaSeTaiLieu_V1 context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Auth/Login
        public IActionResult Login()
        {
            // Nếu đã đăng nhập, chuyển về trang chủ
            if (HttpContext.Session.GetString("MaSinhVien") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            _logger.LogInformation($"=== BẮT ĐẦU ĐĂNG NHẬP ===");
            _logger.LogInformation($"Username nhận được: '{username}'");
            _logger.LogInformation($"Password nhận được: '{password}'");
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Username hoặc password bị null/empty");
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                return View();
            }

            try
            {
                // Trim khoảng trắng thừa
                username = username?.Trim();
                password = password?.Trim();
                
                // Tìm tài khoản
                _logger.LogInformation($"Đang tìm tài khoản với username: '{username}'");
                
                var taiKhoan = await _context.TaiKhoans
                    .Include(tk => tk.MaVaiTroNavigation)
                    .Include(tk => tk.SinhViens)
                    .Include(tk => tk.GiangViens)
                    .FirstOrDefaultAsync(tk => tk.TenTk.Trim() == username);

                if (taiKhoan == null)
                {
                    _logger.LogWarning($"KHÔNG TÌM THẤY tài khoản với username: '{username}'");
                    
                    // Debug: Xem tất cả username trong database
                    var allUsernames = await _context.TaiKhoans.Select(t => t.TenTk).ToListAsync();
                    _logger.LogInformation($"Danh sách username trong DB: {string.Join(", ", allUsernames)}");
                    
                    TempData["ErrorMessage"] = "Tên đăng nhập không tồn tại!";
                    return View();
                }

                _logger.LogInformation($"TÌM THẤY tài khoản: MaTK={taiKhoan.MaTk}, TenTK={taiKhoan.TenTk}");
                _logger.LogInformation($"Password trong DB: '{taiKhoan.MatKhau}'");
                _logger.LogInformation($"Password nhập vào: '{password}'");
                
                // Trim password trong DB trước khi so sánh
                var dbPassword = taiKhoan.MatKhau?.Trim();
                _logger.LogInformation($"So sánh: DB='{dbPassword}' vs Input='{password}' => {dbPassword == password}");

                // Kiểm tra mật khẩu (TODO: Implement password hashing)
                if (dbPassword != password)
                {
                    _logger.LogWarning($"MẬT KHẨU SAI! DB='{dbPassword}' vs Input='{password}'");
                    TempData["ErrorMessage"] = "Mật khẩu không đúng!";
                    return View();
                }

                _logger.LogInformation("Mật khẩu ĐÚNG!");

                // Kiểm tra trạng thái tài khoản
                _logger.LogInformation($"Trạng thái tài khoản: '{taiKhoan.TrangThai}'");
                
                // Kiểm tra nếu tài khoản bị khóa (chỉ chặn các trạng thái khóa rõ ràng)
                if (taiKhoan.TrangThai != null && 
                    (taiKhoan.TrangThai.Contains("khóa", StringComparison.OrdinalIgnoreCase) || 
                     taiKhoan.TrangThai.Contains("Tạm khóa", StringComparison.OrdinalIgnoreCase) ||
                     taiKhoan.TrangThai.Contains("Vô hiệu", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning($"Tài khoản bị khóa: {taiKhoan.TrangThai}");
                    TempData["ErrorMessage"] = "Tài khoản đã bị khóa!";
                    return View();
                }

                // Lưu thông tin vào session
                _logger.LogInformation("Đang lưu thông tin vào Session...");
                HttpContext.Session.SetString("MaTaiKhoan", taiKhoan.MaTk);
                HttpContext.Session.SetString("TenTaiKhoan", taiKhoan.TenTk);
                HttpContext.Session.SetString("MaVaiTro", taiKhoan.MaVaiTro ?? "");
                HttpContext.Session.SetString("TenVaiTro", taiKhoan.MaVaiTroNavigation?.TenVaiTro ?? "");

                // Tạo object để trả về cho client (lưu vào localStorage)
                object userInfo = new
                {
                    MaTaiKhoan = taiKhoan.MaTk,
                    TenTaiKhoan = taiKhoan.TenTk,
                    MaVaiTro = taiKhoan.MaVaiTro,
                    TenVaiTro = taiKhoan.MaVaiTroNavigation?.TenVaiTro,
                    LoaiNguoiDung = "",
                    MaNguoiDung = "",
                    TenNguoiDung = "",
                    Email = "",
                    DiemTichLuy = 0
                };

                // Kiểm tra vai trò và lưu thông tin tương ứng
                if (taiKhoan.SinhViens.Any())
                {
                    var sinhVien = taiKhoan.SinhViens.First();
                    _logger.LogInformation($"Đăng nhập với vai trò SINH VIÊN: {sinhVien.MaSv} - {sinhVien.TenSv}");
                    HttpContext.Session.SetString("MaSinhVien", sinhVien.MaSv);
                    HttpContext.Session.SetString("TenNguoiDung", sinhVien.TenSv);
                    HttpContext.Session.SetString("LoaiNguoiDung", "SinhVien");
                    
                    userInfo = new
                    {
                        MaTaiKhoan = taiKhoan.MaTk,
                        TenTaiKhoan = taiKhoan.TenTk,
                        MaVaiTro = taiKhoan.MaVaiTro,
                        TenVaiTro = taiKhoan.MaVaiTroNavigation?.TenVaiTro,
                        LoaiNguoiDung = "SinhVien",
                        MaNguoiDung = sinhVien.MaSv,
                        TenNguoiDung = sinhVien.TenSv,
                        Email = sinhVien.Email ?? "",
                        NgaySinh = sinhVien.NgaySinh?.ToString("dd/MM/yyyy") ?? "",
                        GioiTinh = sinhVien.GioiTinh ?? "",
                        DiemTichLuy = sinhVien.DiemTichLuy ?? 0,
                        MaLop = sinhVien.MaLop ?? "",
                        TrangThaiSv = sinhVien.TrangThaiSv ?? ""
                    };
                }
                else if (taiKhoan.GiangViens.Any())
                {
                    var giangVien = taiKhoan.GiangViens.First();
                    _logger.LogInformation($"Đăng nhập với vai trò GIẢNG VIÊN: {giangVien.MaGv} - {giangVien.TenGv}");
                    HttpContext.Session.SetString("MaGiangVien", giangVien.MaGv);
                    HttpContext.Session.SetString("TenNguoiDung", giangVien.TenGv);
                    HttpContext.Session.SetString("LoaiNguoiDung", "GiangVien");
                    
                    userInfo = new
                    {
                        MaTaiKhoan = taiKhoan.MaTk,
                        TenTaiKhoan = taiKhoan.TenTk,
                        MaVaiTro = taiKhoan.MaVaiTro,
                        TenVaiTro = taiKhoan.MaVaiTroNavigation?.TenVaiTro,
                        LoaiNguoiDung = "GiangVien",
                        MaNguoiDung = giangVien.MaGv,
                        TenNguoiDung = giangVien.TenGv,
                        Email = giangVien.Email ?? "",
                        NgaySinh = giangVien.NgaySinh?.ToString("dd/MM/yyyy") ?? "",
                        GioiTinh = giangVien.GioiTinh ?? "",
                        Sdt = giangVien.Sdt ?? "",
                        DiemTichLuy = 0,
                        MaKhoa = giangVien.MaKhoa ?? ""
                    };
                }
                else
                {
                    _logger.LogWarning("Tài khoản không có SinhVien hoặc GiangVien liên kết!");
                }

                _logger.LogInformation("=== ĐĂNG NHẬP THÀNH CÔNG ===");
                
                // Kiểm tra nếu là AJAX request thì trả về JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đăng nhập thành công!", userInfo });
                }
                
                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                TempData["UserInfo"] = System.Text.Json.JsonSerializer.Serialize(userInfo);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI KHI ĐĂNG NHẬP");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại!";
                return View();
            }
        }

        // GET: Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            TempData["ClearLocalStorage"] = "true"; // Signal để xóa localStorage
            return RedirectToAction("Login");
        }

        // GET: Auth/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword, 
            string tenSv, string email, string maSv)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || 
                string.IsNullOrEmpty(tenSv) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(maSv))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự!";
                return View();
            }

            try
            {
                // Kiểm tra tên đăng nhập đã tồn tại
                var existingAccount = await _context.TaiKhoans
                    .FirstOrDefaultAsync(tk => tk.TenTk == username);

                if (existingAccount != null)
                {
                    TempData["ErrorMessage"] = "Tên đăng nhập đã tồn tại!";
                    return View();
                }

                // Kiểm tra mã sinh viên đã tồn tại
                var existingSinhVien = await _context.SinhViens
                    .FirstOrDefaultAsync(sv => sv.MaSv == maSv);

                if (existingSinhVien != null)
                {
                    TempData["ErrorMessage"] = "Mã sinh viên đã được đăng ký!";
                    return View();
                }

                // Lấy vai trò sinh viên
                var vaiTroSinhVien = await _context.VaiTros
                    .FirstOrDefaultAsync(vt => vt.TenVaiTro == "Sinh viên");

                if (vaiTroSinhVien == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy vai trò sinh viên trong hệ thống!";
                    return View();
                }

                // Tạo tài khoản mới
                var taiKhoan = new TaiKhoan
                {
                    MaTk = GenerateMaTaiKhoan(),
                    TenTk = username,
                    MatKhau = password, // TODO: Hash password
                    MaVaiTro = vaiTroSinhVien.MaVaiTro,
                    TrangThai = "Hoạt động"
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();

                // Tạo sinh viên mới
                var sinhVien = new SinhVien
                {
                    MaSv = maSv,
                    TenSv = tenSv,
                    Email = email,
                    MaTk = taiKhoan.MaTk,
                    DiemTichLuy = 0,
                    TrangThaiSv = "Đang học"
                };

                _context.SinhViens.Add(sinhVien);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại!";
                return View();
            }
        }

        private string GenerateMaTaiKhoan()
        {
            var lastTaiKhoan = _context.TaiKhoans
                .OrderByDescending(tk => tk.MaTk)
                .FirstOrDefault();

            if (lastTaiKhoan == null)
            {
                return "TK001";
            }

            var lastNumber = int.Parse(lastTaiKhoan.MaTk.Substring(2));
            return $"TK{(lastNumber + 1):D3}";
        }

        // GET: Auth/TestAccount?username=sv_mai
        public async Task<IActionResult> TestAccount(string username)
        {
            _logger.LogInformation($"=== TEST TÀI KHOẢN: {username} ===");
            
            // Lấy tất cả tài khoản
            var allAccounts = await _context.TaiKhoans
                .Include(tk => tk.MaVaiTroNavigation)
                .Include(tk => tk.SinhViens)
                .Include(tk => tk.GiangViens)
                .ToListAsync();
            
            _logger.LogInformation($"Tổng số tài khoản: {allAccounts.Count}");
            
            foreach (var acc in allAccounts)
            {
                _logger.LogInformation($"- MaTK: {acc.MaTk}, TenTK: '{acc.TenTk}', MatKhau: '{acc.MatKhau}', TrangThai: '{acc.TrangThai}'");
            }
            
            // Tìm tài khoản cụ thể
            var taiKhoan = allAccounts.FirstOrDefault(tk => tk.TenTk.Trim() == username?.Trim());
            
            if (taiKhoan == null)
            {
                _logger.LogWarning($"KHÔNG TÌM THẤY tài khoản: {username}");
                return Content($"KHÔNG TÌM THẤY tài khoản: {username}");
            }
            
            _logger.LogInformation($"TÌM THẤY tài khoản:");
            _logger.LogInformation($"- MaTK: {taiKhoan.MaTk}");
            _logger.LogInformation($"- TenTK: '{taiKhoan.TenTk}'");
            _logger.LogInformation($"- MatKhau: '{taiKhoan.MatKhau}'");
            _logger.LogInformation($"- TrangThai: '{taiKhoan.TrangThai}'");
            _logger.LogInformation($"- MaVaiTro: '{taiKhoan.MaVaiTro}'");
            _logger.LogInformation($"- Số SinhVien: {taiKhoan.SinhViens.Count}");
            _logger.LogInformation($"- Số GiangVien: {taiKhoan.GiangViens.Count}");
            
            if (taiKhoan.SinhViens.Any())
            {
                var sv = taiKhoan.SinhViens.First();
                _logger.LogInformation($"- SinhVien: MaSV={sv.MaSv}, TenSV={sv.TenSv}");
            }
            
            return Content($"Tìm thấy tài khoản: {taiKhoan.TenTk} - Xem log để biết chi tiết");
        }

        // GET: Auth/TestLogin?username=sv_mai&password=123456
        public async Task<IActionResult> TestLogin(string username, string password)
        {
            _logger.LogInformation($"=== TEST ĐĂNG NHẬP: {username} / {password} ===");
            
            try
            {
                // Trim khoảng trắng thừa
                username = username?.Trim();
                password = password?.Trim();
                
                // Tìm tài khoản
                var taiKhoan = await _context.TaiKhoans
                    .Include(tk => tk.MaVaiTroNavigation)
                    .Include(tk => tk.SinhViens)
                    .Include(tk => tk.GiangViens)
                    .FirstOrDefaultAsync(tk => tk.TenTk.Trim() == username);

                if (taiKhoan == null)
                {
                    _logger.LogWarning($"KHÔNG TÌM THẤY tài khoản");
                    return Content("KHÔNG TÌM THẤY tài khoản");
                }

                _logger.LogInformation($"TÌM THẤY tài khoản: {taiKhoan.TenTk}");
                
                var dbPassword = taiKhoan.MatKhau?.Trim();
                _logger.LogInformation($"So sánh password: DB='{dbPassword}' vs Input='{password}'");

                if (dbPassword != password)
                {
                    _logger.LogWarning($"MẬT KHẨU SAI!");
                    return Content("MẬT KHẨU SAI!");
                }

                _logger.LogInformation("Mật khẩu ĐÚNG!");
                _logger.LogInformation($"Trạng thái: '{taiKhoan.TrangThai}'");
                
                // Kiểm tra trạng thái
                if (taiKhoan.TrangThai != null && 
                    (taiKhoan.TrangThai.Contains("khóa", StringComparison.OrdinalIgnoreCase) || 
                     taiKhoan.TrangThai.Contains("Tạm khóa", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning($"Tài khoản bị khóa");
                    return Content("Tài khoản bị khóa");
                }

                _logger.LogInformation("Trạng thái OK!");
                
                if (taiKhoan.SinhViens.Any())
                {
                    var sv = taiKhoan.SinhViens.First();
                    _logger.LogInformation($"SinhVien: {sv.MaSv} - {sv.TenSv}");
                    return Content($"ĐĂNG NHẬP THÀNH CÔNG! Sinh viên: {sv.TenSv}");
                }
                
                return Content("ĐĂNG NHẬP THÀNH CÔNG!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI");
                return Content($"LỖI: {ex.Message}");
            }
        }
    }
}
