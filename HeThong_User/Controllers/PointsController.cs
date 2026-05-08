using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_User.Controllers
{
    public class PointsController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;
        private readonly ILogger<PointsController> _logger;

        public PointsController(HeThongChiaSeTaiLieu_V1 context, ILogger<PointsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Points/Index
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

        // GET: Points/GetPointsHistory - Lấy lịch sử điểm
        [HttpGet]
        public async Task<IActionResult> GetPointsHistory()
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

                // Lấy thông tin sinh viên để có tổng điểm hiện tại
                var sinhVien = await _context.SinhViens.FindAsync(maNguoiDung);
                var tongDiem = sinhVien?.DiemTichLuy ?? 0;

                // Lấy lịch sử điểm từ bảng LichSuDiem
                var lichSuDiem = await _context.LichSuDiems
                    .Include(ls => ls.MaHkNavigation)
                    .Where(ls => ls.MaSv == maNguoiDung)
                    .OrderByDescending(ls => ls.MaLS)
                    .ToListAsync();

                // Tính điểm sau thay đổi cho từng bản ghi
                var tongDiemHienTai = sinhVien?.DiemTichLuy ?? 0;
                var lichSuDiemResult = new List<object>();
                
                foreach (var ls in lichSuDiem)
                {
                    lichSuDiemResult.Add(new
                    {
                        maLs = ls.MaLS,
                        diemThayDoi = ls.SoDiemThayDoi,
                        diemSauThayDoi = tongDiemHienTai, // Điểm hiện tại
                        lyDo = ls.LyDo,
                        ngayThayDoi = ls.NgayThayDoi,
                        hocKy = ls.MaHkNavigation != null ? ls.MaHkNavigation.TenHk : null
                    });
                    
                    // Trừ ngược lại để tính điểm trước đó
                    tongDiemHienTai -= (ls.SoDiemThayDoi ?? 0);
                }

                return Json(new 
                { 
                    success = true, 
                    tongDiem = tongDiem,
                    lichSu = lichSuDiemResult 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử điểm");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Points/GetPointsStats - Thống kê điểm
        [HttpGet]
        public async Task<IActionResult> GetPointsStats()
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

                // Tổng điểm hiện tại
                var sinhVien = await _context.SinhViens.FindAsync(maNguoiDung);
                var tongDiem = sinhVien?.DiemTichLuy ?? 0;

                // Lấy học kỳ hiện tại (học kỳ mới nhất)
                var hocKyHienTai = await _context.HocKies
                    .OrderByDescending(hk => hk.MaHk)
                    .FirstOrDefaultAsync();

                // Số bài đã đăng (đếm từ bảng TaiLieu)
                var soBaiDang = await _context.TaiLieus
                    .Where(tl => tl.MaNguoiDang == maNguoiDung)
                    .CountAsync();

                // Xếp hạng
                var allStudents = await _context.SinhViens
                    .OrderByDescending(sv => sv.DiemTichLuy)
                    .Select(sv => sv.MaSv)
                    .ToListAsync();
                
                var xepHang = allStudents.IndexOf(maNguoiDung) + 1;
                var tongSinhVien = allStudents.Count;

                return Json(new 
                { 
                    success = true,
                    tongDiem = tongDiem,
                    diemHocKyHienTai = tongDiem, // Điểm tích lũy = điểm học kỳ hiện tại
                    tenHocKyHienTai = hocKyHienTai?.TenHk ?? "Chưa có học kỳ",
                    soBaiDang = soBaiDang,
                    xepHang = xepHang,
                    tongSinhVien = tongSinhVien
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê điểm");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }
    }
}
