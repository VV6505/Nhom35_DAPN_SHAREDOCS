using System.Diagnostics;
using HeThong_Admin.Models;
using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_Admin.Controllers
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

        public async Task<IActionResult> Index()
        {
            var colors = new[] { "#3b82f6", "#06b6d4", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899", "#14b8a6" };

            var tongTaiLieu = await _context.TaiLieus.CountAsync();
            var dangChoDuyet = await _context.TaiLieus.CountAsync(t => t.TrangThaiDuyet == "Chờ duyệt");
            var tongSinhVien = await _context.SinhViens.CountAsync();
            var tongGiangVien = await _context.GiangViens.CountAsync();
            var tongBaoCao = await _context.BaoCaoViPhams.CountAsync();

            var taiLieuTheoKhoa = await _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)
                    .ThenInclude(m => m!.MaNganhNavigation)
                        .ThenInclude(n => n!.MaKhoaNavigation)
                .Where(t => t.MaMonHocNavigation != null
                    && t.MaMonHocNavigation.MaNganhNavigation != null
                    && t.MaMonHocNavigation.MaNganhNavigation.MaKhoaNavigation != null)
                .GroupBy(t => t.MaMonHocNavigation!.MaNganhNavigation!.MaKhoaNavigation!.TenKhoa)
                .Select(g => new TaiLieuTheoKhoa
                {
                    TenKhoa = g.Key,
                    SoTaiLieu = g.Count()
                })
                .OrderByDescending(x => x.SoTaiLieu)
                .ToListAsync();

            for (int i = 0; i < taiLieuTheoKhoa.Count; i++)
                taiLieuTheoKhoa[i].MauSac = colors[i % colors.Length];

            var topSinhVien = await _context.SinhViens
                .Include(s => s.MaLopNavigation)
                .OrderByDescending(s => s.DiemTichLuy)
                .Take(5)
                .Select(s => new TopSinhVien
                {
                    TenSV = s.TenSv,
                    DiemTichLuy = s.DiemTichLuy ?? 0,
                    Lop = s.MaLopNavigation != null ? s.MaLopNavigation.TenLop : "—"
                })
                .ToListAsync();

            var allBaoCao = await _context.BaoCaoViPhams.ToListAsync();
            var baoCaoTheoLyDo = allBaoCao
                .GroupBy(b => string.IsNullOrEmpty(b.LyDo) ? "Khác" : b.LyDo)
                .Select(g => new BaoCaoTheoLyDo
                {
                    LyDo = g.Key,
                    SoBaoCao = g.Count(),
                    ChoXuLy = g.Count(x => x.TrangThaiXuLy == null || x.TrangThaiXuLy == "Chờ xử lý" || x.TrangThaiXuLy == "ChờXửLý"),
                    DaXuLy = g.Count(x => x.TrangThaiXuLy == "Đã xử lý" || x.TrangThaiXuLy == "Bỏ qua")
                })
                .OrderByDescending(x => x.SoBaoCao)
                .ToList();

            var viewModel = new DashboardViewModel
            {
                TongTaiLieu = tongTaiLieu,
                DangChoDuyet = dangChoDuyet,
                TongSinhVien = tongSinhVien,
                TongGiangVien = tongGiangVien,
                TongBaoCaoViPham = tongBaoCao,
                TaiLieuTheoKhoas = taiLieuTheoKhoa,
                TopSinhViens = topSinhVien,
                BaoCaoTheoLyDos = baoCaoTheoLyDo
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> XuatBaoCao(string LoaiBaoCao, string DinhDang, DateTime TuNgay, DateTime DenNgay)
        {
            var endOfDay = DenNgay.AddDays(1).AddTicks(-1);
            var sb = new System.Text.StringBuilder();

            if (LoaiBaoCao == "TaiLieu")
            {
                var data = await _context.TaiLieus
                    .Where(t => t.NgayDang >= TuNgay && t.NgayDang <= endOfDay)
                    .Select(t => new { t.MaTaiLieu, t.TieuDe, t.TrangThaiDuyet, NgayDang = t.NgayDang.HasValue ? t.NgayDang.Value.ToString("yyyy-MM-dd") : "" })
                    .ToListAsync();
                sb.AppendLine("MaTaiLieu,TieuDe,TrangThai,NgayDang");
                foreach (var item in data)
                    sb.AppendLine($"{item.MaTaiLieu},{item.TieuDe?.Replace(",", " ")},{item.TrangThaiDuyet},{item.NgayDang}");
            }
            else if (LoaiBaoCao == "NguoiDung")
            {
                var data = await _context.TaiKhoans
                    .Select(t => new { t.MaTk, t.TenTk, t.TrangThai })
                    .ToListAsync();
                sb.AppendLine("MaTaiKhoan,TenTaiKhoan,TrangThai");
                foreach (var item in data)
                    sb.AppendLine($"{item.MaTk},{item.TenTk?.Replace(",", " ")},{item.TrangThai}");
            }
            else if (LoaiBaoCao == "ViPham")
            {
                var data = await _context.BaoCaoViPhams
                    .Where(b => b.NgayBaoCao >= TuNgay && b.NgayBaoCao <= endOfDay)
                    .Select(b => new { b.MaBaoCao, b.LyDo, b.TrangThaiXuLy, NgayBC = b.NgayBaoCao.HasValue ? b.NgayBaoCao.Value.ToString("yyyy-MM-dd") : "" })
                    .ToListAsync();
                sb.AppendLine("MaBaoCao,LyDo,TrangThai,NgayBaoCao");
                foreach (var item in data)
                    sb.AppendLine($"{item.MaBaoCao},{item.LyDo?.Replace(",", " ")},{item.TrangThaiXuLy},{item.NgayBC}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var result = new byte[bom.Length + bytes.Length];
            System.Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            System.Buffer.BlockCopy(bytes, 0, result, bom.Length, bytes.Length);

            var fileName = $"BaoCao_{LoaiBaoCao}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(result, "text/csv", fileName);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new HeThong_Admin.Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
