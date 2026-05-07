using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThong_User.Models;

namespace HeThong_User.Controllers
{
    public class HomeController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public HomeController(HeThongChiaSeTaiLieu_V1 context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Giữ nguyên các con số thống kê cho Header/Sidebar
            ViewBag.TotalDocs = _context.TaiLieus.Count();
            ViewBag.TotalUsers = _context.SinhViens.Count();
            ViewBag.TotalDownloads = _context.TaiLieus.Sum(t => t.LuotTai) ?? 0;
            ViewBag.TotalReports = _context.BaoCaoViPhams.Count(); // Thêm đếm báo cáo cho ô màu đỏ

            // 2. Lấy danh sách tài liệu cho Bảng tin
            // Mình sẽ lấy 10 tài liệu mới nhất để bảng tin nhìn dài và đầy đặn hơn
            var newsfeedDocs = _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)   // Để hiện tên môn học (VD: CTDL&GT)
                .Include(t => t.MaloaiTlNavigation)   // Để hiện loại (VD: Giáo trình)
                                                      // Nếu trong Model của bạn đã tạo quan hệ (Foreign Key) với SinhVien, hãy dùng thêm:
                                                      // .Include(t => t.MaNguoiDangNavigation) 
                .Where(t => t.TrangThaiDuyet == "DaDuyet")
                .OrderByDescending(t => t.NgayDang)
                .Take(10)
                .ToList();  

            return View(newsfeedDocs);
        }
        public IActionResult Students()
        {
            var dsSinhVien = _context.SinhViens.Include(s => s.MaLopNavigation).ToList();
            return View(dsSinhVien);
        }
        public IActionResult History()
        {
            var lichSu = _context.LichSuTaiXuongs
                .Include(l => l.MaTaiLieuNavigation)
                .OrderByDescending(l => l.NgayTai)
                .ToList();
            return View(lichSu);
        }
    }
}