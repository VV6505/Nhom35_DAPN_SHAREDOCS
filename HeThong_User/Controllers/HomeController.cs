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
            // 1. Lấy các con số thống kê
            ViewBag.TotalDocs = _context.TaiLieus.Count();
            ViewBag.TotalUsers = _context.SinhViens.Count();
            ViewBag.TotalDownloads = _context.TaiLieus.Sum(t => t.LuotTai) ?? 0;

            // 2. Lấy 6 tài liệu mới nhất (đã duyệt)
            var latestDocs = _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)
                .Include(t => t.MaloaiTlNavigation)
                .Where(t => t.TrangThaiDuyet == "DaDuyet")
                .OrderByDescending(t => t.NgayDang)
                .Take(6)
                .ToList();

            return View(latestDocs);
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