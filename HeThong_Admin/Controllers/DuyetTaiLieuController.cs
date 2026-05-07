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
            var adminId = HttpContext.Session.GetString("AdminId");
            var adminRole = HttpContext.Session.GetString("AdminRole");

            var query = _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)
                    .ThenInclude(m => m!.MaNganhNavigation)
                        .ThenInclude(n => n!.MaKhoaNavigation)
                .AsQueryable();

            // Logic theo vai trò
            if (adminRole == "VT004") // Cán bộ khoa
            {
                // Lấy khoa của CBK
                var tk = await _context.TaiKhoans
                    .Include(x => x.MaGvNavigation)
                    .FirstOrDefaultAsync(x => x.MaTk == adminId);
                
                var maKhoaCBK = tk?.MaGvNavigation?.MaKhoa;

                if (!string.IsNullOrEmpty(maKhoaCBK))
                {
                    // CBK chỉ thấy tài liệu thuộc khoa mình và đang ở trạng thái Chờ duyệt
                    query = query.Where(t => t.MaMonHocNavigation != null
                        && t.MaMonHocNavigation.MaNganhNavigation != null
                        && t.MaMonHocNavigation.MaNganhNavigation.MaKhoa == maKhoaCBK);

                    if (string.IsNullOrEmpty(trangthai) || trangthai == "all")
                        query = query.Where(t => t.TrangThaiDuyet == "Chờ duyệt");
                }
            }
            else if (adminRole == "VT001") // Admin
            {
                // Admin thấy tài liệu Chờ Admin duyệt (từ khoa gửi lên) 
                // Hoặc tài liệu Chờ duyệt mà không thuộc khoa nào (tài liệu ngoài)
                if (string.IsNullOrEmpty(trangthai) || trangthai == "all")
                {
                    query = query.Where(t => t.TrangThaiDuyet == "Chờ Admin duyệt" || t.TrangThaiDuyet == "Chờ duyệt");
                }
            }

            // Lọc trạng thái (nếu chọn cụ thể)
            if (!string.IsNullOrEmpty(trangthai) && trangthai != "all")
            {
                query = query.Where(t => t.TrangThaiDuyet == trangthai);
            }

            // Lọc theo khoa (dành cho Admin)
            if (adminRole == "VT001" && !string.IsNullOrEmpty(khoa) && khoa != "all")
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
            var adminRole = HttpContext.Session.GetString("AdminRole");
            var tl = await _context.TaiLieus.FindAsync(id);
            
            if (tl != null)
            {
                if (adminRole == "VT004") // Cán bộ khoa duyệt -> Gửi lên Admin
                {
                    tl.TrangThaiDuyet = "Chờ Admin duyệt";
                }
                else if (adminRole == "VT001") // Admin duyệt -> Xong
                {
                    tl.TrangThaiDuyet = "Đã duyệt";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TuChoi(string id, string lyDo)
        {
            var tl = await _context.TaiLieus.FindAsync(id);
            if (tl != null)
            {
                tl.TrangThaiDuyet = "Từ chối";
                tl.LyDoTuChoi = lyDo;
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
