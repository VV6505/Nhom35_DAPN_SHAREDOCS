using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_Admin.Controllers
{
    public class NguoiDungController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public NguoiDungController(HeThongChiaSeTaiLieu_V1 context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? vaitro, string? trangthai)
        {
            var query = _context.TaiKhoans
                .Include(t => t.MaVaiTroNavigation)
                .Include(t => t.SinhViens)
                .Include(t => t.GiangViens)
                .AsQueryable();

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.TenTk.Contains(search) ||
                    t.SinhViens.Any(s => s.TenSv.Contains(search)) ||
                    t.GiangViens.Any(g => g.TenGv.Contains(search)));
            }

            // Lọc theo vai trò
            if (!string.IsNullOrEmpty(vaitro) && vaitro != "all")
            {
                query = query.Where(t => t.MaVaiTro == vaitro);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangthai) && trangthai != "all")
            {
                query = query.Where(t => t.TrangThai == trangthai);
            }

            var users = await query.OrderBy(t => t.MaTk).ToListAsync();
            var vaiTros = await _context.VaiTros.ToListAsync();

            ViewBag.VaiTros = vaiTros;
            ViewBag.Search = search;
            ViewBag.VaiTro = vaitro;
            ViewBag.TrangThai = trangthai;

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> KhoaTaiKhoan(string id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk != null)
            {
                tk.TrangThai = "Tạm khóa";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MoKhoaTaiKhoan(string id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk != null)
            {
                tk.TrangThai = "Đang hoạt động";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> XoaTaiKhoan(string id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk != null)
            {
                _context.TaiKhoans.Remove(tk);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
