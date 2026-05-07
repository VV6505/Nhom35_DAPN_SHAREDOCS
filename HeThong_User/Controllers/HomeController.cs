using System.Diagnostics;
using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_User.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public HomeController(ILogger<HomeController> logger, HeThongChiaSeTaiLieu_V1 context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = HttpContext.Session.GetString("MaTaiKhoan");
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false });

            var notifications = await _context.ThongBaos
                .Where(t => t.MaNguoiNhan == userId)
                .OrderByDescending(t => t.NgayTao)
                .Take(10)
                .Select(t => new {
                    t.MaTb,
                    t.TieuDe,
                    t.NoiDung,
                    t.TrangThai,
                    NgayTao = t.NgayTao.HasValue ? t.NgayTao.Value.ToString("dd/MM/yyyy HH:mm") : ""
                })
                .ToListAsync();

            return Json(new { success = true, data = notifications });
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
