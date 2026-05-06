using HeThong_User.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThong_User.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(HeThongChiaSeTaiLieu_V1 context, ILogger<DocumentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Documents/MyDocuments
        public IActionResult MyDocuments()
        {
            // Kiểm tra session
            var maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan");
            if (string.IsNullOrEmpty(maTaiKhoan))
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // GET: Documents/GetMyUploads - Lấy tài liệu đã đăng
        [HttpGet]
        public async Task<IActionResult> GetMyUploads()
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

                var taiLieuDaDang = await _context.TaiLieus
                    .Include(tl => tl.MaMonHocNavigation)
                    .Include(tl => tl.MaloaiTlNavigation)
                    .Where(tl => tl.MaNguoiDang == maNguoiDung)
                    .OrderByDescending(tl => tl.NgayDang)
                    .Select(tl => new
                    {
                        maTaiLieu = tl.MaTaiLieu,
                        tieuDe = tl.TieuDe,
                        moTa = tl.MoTa,
                        loaiFile = tl.LoaiFile,
                        kichThuoc = tl.KichThuoc,
                        luotTai = tl.LuotTai,
                        ngayDang = tl.NgayDang,
                        trangThaiDuyet = tl.TrangThaiDuyet,
                        tenMonHoc = tl.MaMonHocNavigation != null ? tl.MaMonHocNavigation.TenMonHoc : null,
                        loaiTaiLieu = tl.MaloaiTlNavigation != null ? tl.MaloaiTlNavigation.TenLtl : null,
                        diemYeuCau = tl.DiemYeuCau
                    })
                    .ToListAsync();

                return Json(new { success = true, taiLieu = taiLieuDaDang });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tài liệu đã đăng");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Documents/GetMySaved - Lấy tài liệu đã lưu
        [HttpGet]
        public async Task<IActionResult> GetMySaved()
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

                var taiLieuDaLuu = await _context.TlyeuThiches
                    .Include(tlt => tlt.MaTlNavigation)
                        .ThenInclude(tl => tl.MaMonHocNavigation)
                    .Include(tlt => tlt.MaTlNavigation)
                        .ThenInclude(tl => tl.MaloaiTlNavigation)
                    .Where(tlt => tlt.MaNd == maNguoiDung)
                    .OrderByDescending(tlt => tlt.ThoiGian)
                    .Select(tlt => new
                    {
                        maTll = tlt.MaTll,
                        thoiGianLuu = tlt.ThoiGian,
                        taiLieu = tlt.MaTlNavigation != null ? new
                        {
                            maTaiLieu = tlt.MaTlNavigation.MaTaiLieu,
                            tieuDe = tlt.MaTlNavigation.TieuDe,
                            moTa = tlt.MaTlNavigation.MoTa,
                            loaiFile = tlt.MaTlNavigation.LoaiFile,
                            kichThuoc = tlt.MaTlNavigation.KichThuoc,
                            luotTai = tlt.MaTlNavigation.LuotTai,
                            ngayDang = tlt.MaTlNavigation.NgayDang,
                            tenMonHoc = tlt.MaTlNavigation.MaMonHocNavigation != null ? tlt.MaTlNavigation.MaMonHocNavigation.TenMonHoc : null,
                            loaiTaiLieu = tlt.MaTlNavigation.MaloaiTlNavigation != null ? tlt.MaTlNavigation.MaloaiTlNavigation.TenLtl : null,
                            diemYeuCau = tlt.MaTlNavigation.DiemYeuCau
                        } : null
                    })
                    .ToListAsync();

                return Json(new { success = true, taiLieu = taiLieuDaLuu });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tài liệu đã lưu");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Documents/GetMyDownloads - Lấy lịch sử tải xuống
        [HttpGet]
        public async Task<IActionResult> GetMyDownloads()
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

                var lichSuTaiXuong = await _context.LichSuTaiXuongs
                    .Include(ls => ls.MaTaiLieuNavigation)
                        .ThenInclude(tl => tl.MaMonHocNavigation)
                    .Include(ls => ls.MaTaiLieuNavigation)
                        .ThenInclude(tl => tl.MaloaiTlNavigation)
                    .Where(ls => ls.MaNd == maNguoiDung)
                    .OrderByDescending(ls => ls.NgayTai)
                    .Select(ls => new
                    {
                        maDownTl = ls.MaDownTl,
                        ngayTai = ls.NgayTai,
                        taiLieu = ls.MaTaiLieuNavigation != null ? new
                        {
                            maTaiLieu = ls.MaTaiLieuNavigation.MaTaiLieu,
                            tieuDe = ls.MaTaiLieuNavigation.TieuDe,
                            moTa = ls.MaTaiLieuNavigation.MoTa,
                            loaiFile = ls.MaTaiLieuNavigation.LoaiFile,
                            kichThuoc = ls.MaTaiLieuNavigation.KichThuoc,
                            luotTai = ls.MaTaiLieuNavigation.LuotTai,
                            ngayDang = ls.MaTaiLieuNavigation.NgayDang,
                            tenMonHoc = ls.MaTaiLieuNavigation.MaMonHocNavigation != null ? ls.MaTaiLieuNavigation.MaMonHocNavigation.TenMonHoc : null,
                            loaiTaiLieu = ls.MaTaiLieuNavigation.MaloaiTlNavigation != null ? ls.MaTaiLieuNavigation.MaloaiTlNavigation.TenLtl : null,
                            diemYeuCau = ls.MaTaiLieuNavigation.DiemYeuCau
                        } : null
                    })
                    .ToListAsync();

                return Json(new { success = true, lichSu = lichSuTaiXuong });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử tải xuống");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }
    }
}
