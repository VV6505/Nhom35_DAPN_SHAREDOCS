using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_Admin.Controllers
{
    public class DuyetTaiLieuController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public DuyetTaiLieuController(HeThongChiaSeTaiLieu_V1 context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? trangthai, string? khoa, string? search)
        {
            var query = _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)
                    .ThenInclude(m => m!.MaNganhNavigation)
                        .ThenInclude(n => n!.MaKhoaNavigation)
                .AsQueryable();

            // Lọc trạng thái
            if (!string.IsNullOrEmpty(trangthai) && trangthai != "all")
            {
                query = query.Where(t => t.TrangThaiDuyet == trangthai);
            }

            // Lọc theo khoa
            if (!string.IsNullOrEmpty(khoa) && khoa != "all")
            {
                query = query.Where(t => t.MaMonHocNavigation != null
                    && t.MaMonHocNavigation.MaNganhNavigation != null
                    && t.MaMonHocNavigation.MaNganhNavigation.MaKhoa == khoa);
            }

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.TieuDe.Contains(search));
            }

            var taiLieus = await query.OrderByDescending(t => t.NgayDang).ToListAsync();
            var khoas = await _context.Khoas.OrderBy(k => k.TenKhoa).ToListAsync();

            // Lấy tên người đăng
            var nguoiDangMap = new Dictionary<string, string>();
            foreach (var tl in taiLieus)
            {
                if (!string.IsNullOrEmpty(tl.MaNguoiDang) && !nguoiDangMap.ContainsKey(tl.MaNguoiDang))
                {
                    var sv = await _context.SinhViens.FirstOrDefaultAsync(s => s.MaSv == tl.MaNguoiDang);
                    var gv = await _context.GiangViens.FirstOrDefaultAsync(g => g.MaGv == tl.MaNguoiDang);
                    nguoiDangMap[tl.MaNguoiDang] = sv?.TenSv ?? gv?.TenGv ?? tl.MaNguoiDang;
                }
            }

            ViewBag.Khoas = khoas;
            ViewBag.TrangThai = trangthai;
            ViewBag.Khoa = khoa;
            ViewBag.Search = search;
            ViewBag.NguoiDangMap = nguoiDangMap;

            return View(taiLieus);
        }

        [HttpPost]
        public async Task<IActionResult> Duyet(string id)
        {
            var tl = await _context.TaiLieus.FindAsync(id);
            if (tl != null)
            {
                tl.TrangThaiDuyet = "Đã duyệt";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TuChoi(string id)
        {
            var tl = await _context.TaiLieus.FindAsync(id);
            if (tl != null)
            {
                tl.TrangThaiDuyet = "Từ chối";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Xoa(string id)
        {
            var tl = await _context.TaiLieus.FindAsync(id);
            if (tl != null)
            {
                _context.TaiLieus.Remove(tl);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
