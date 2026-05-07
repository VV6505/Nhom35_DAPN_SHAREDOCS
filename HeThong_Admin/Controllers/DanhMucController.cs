using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_Admin.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public DanhMucController(HeThongChiaSeTaiLieu_V1 context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? selectedKhoa, string? selectedNganh, string? selectedMonHoc)
        {
            var khoas = await _context.Khoas
                .Include(k => k.Nganhs)
                    .ThenInclude(n => n.MonHocs)
                        .ThenInclude(m => m.TaiLieus)
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.SelectedKhoa = selectedKhoa;
            ViewBag.SelectedNganh = selectedNganh;
            ViewBag.SelectedMonHoc = selectedMonHoc;

            return View(khoas);
        }

        [HttpPost]
        public async Task<IActionResult> ThemKhoa(string tenKhoa)
        {
            if (!string.IsNullOrEmpty(tenKhoa))
            {
                var maxId = await _context.Khoas.CountAsync() + 1;
                var maKhoa = "K" + maxId.ToString("D4");
                _context.Khoas.Add(new Khoa { MaKhoa = maKhoa, TenKhoa = tenKhoa });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ThemNganh(string tenNganh, string maKhoa)
        {
            if (!string.IsNullOrEmpty(tenNganh) && !string.IsNullOrEmpty(maKhoa))
            {
                var maxId = await _context.Nganhs.CountAsync() + 1;
                var maNganh = "NG" + maxId.ToString("D3");
                _context.Nganhs.Add(new Nganh { MaNganh = maNganh, TenNganh = tenNganh, MaKhoa = maKhoa });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ThemMonHoc(string tenMonHoc, string maNganh)
        {
            if (!string.IsNullOrEmpty(tenMonHoc) && !string.IsNullOrEmpty(maNganh))
            {
                var maxId = await _context.MonHocs.CountAsync() + 1;
                var maMH = maxId.ToString("D4") + "0";
                _context.MonHocs.Add(new MonHoc { MaMonHoc = maMH, TenMonHoc = tenMonHoc, MaNganh = maNganh });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SuaMonHoc(string maMonHoc, string tenMonHoc)
        {
            if (!string.IsNullOrEmpty(maMonHoc) && !string.IsNullOrEmpty(tenMonHoc))
            {
                var item = await _context.MonHocs.FindAsync(maMonHoc);
                if (item != null)
                {
                    item.TenMonHoc = tenMonHoc;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> XoaDanhMuc(string type, string id)
        {
            if (type == "khoa")
            {
                var item = await _context.Khoas.FindAsync(id);
                if (item != null) { _context.Khoas.Remove(item); await _context.SaveChangesAsync(); }
            }
            else if (type == "nganh")
            {
                var item = await _context.Nganhs.FindAsync(id);
                if (item != null) { _context.Nganhs.Remove(item); await _context.SaveChangesAsync(); }
            }
            else if (type == "monhoc")
            {
                var item = await _context.MonHocs.FindAsync(id);
                if (item != null) { _context.MonHocs.Remove(item); await _context.SaveChangesAsync(); }
            }
            return RedirectToAction("Index");
        }
    }
}
