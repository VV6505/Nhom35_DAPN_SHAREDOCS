    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using HeThong_User.Models;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using System.IO;
    using System.Threading.Tasks;

    namespace HeThong_User.Controllers
    {
        public class DocumentController : Controller
        {
            private readonly HeThongChiaSeTaiLieu_V1 _context;
            private readonly IWebHostEnvironment _env;
            private readonly ILogger<DocumentController> _logger;

            public DocumentController(HeThongChiaSeTaiLieu_V1 context, IWebHostEnvironment env, ILogger<DocumentController> logger)
            {
                _context = context;
                _env = env;
                _logger = logger;
            }

            // =================================================================
            // INDEX: Danh mục tài liệu (Cây thư mục + Lấy tên thật người đăng)
            // =================================================================
            public IActionResult Index(string idMon, string searchString)
            {
                // 1. Lấy dữ liệu phân cấp cho Sidebar (Khoa -> Ngành -> Môn)
                ViewBag.DanhMuc = _context.Khoas
                    .Include(k => k.Nganhs)
                        .ThenInclude(n => n.MonHocs)
                            .ThenInclude(m => m.TaiLieus)
                    .ToList();

                // 2. Truy vấn lấy Tài liệu + Join lấy Tên Sinh Viên (Thông qua TaiKhoan)
                var query = from t in _context.TaiLieus
                            join tk in _context.TaiKhoans on t.MaNguoiDang equals tk.MaTk into tkGroup
                            from tk in tkGroup.DefaultIfEmpty()
                            join sv in _context.SinhViens on tk.MaSv equals sv.MaSv into svGroup
                            from sv in svGroup.DefaultIfEmpty()
                            where t.TrangThaiDuyet == "Đã duyệt"
                            select new
                            {
                                Data = t,
                                TenNguoiDang = sv != null ? sv.TenSv : (tk != null ? tk.TenTk : t.MaNguoiDang),
                                TenMon = t.MaMonHocNavigation.TenMonHoc
                            };

                // 3. Lọc theo Môn học nếu người dùng nhấn vào cây thư mục
                if (!string.IsNullOrEmpty(idMon))
                {
                    query = query.Where(x => x.Data.MaMonHoc == idMon);
                    ViewBag.CurrentMon = idMon;
                }

                // 4. Lọc theo từ khóa tìm kiếm
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(x => x.Data.TieuDe.Contains(searchString) || x.Data.MoTa.Contains(searchString));
                    ViewBag.SearchString = searchString;
                }

                var results = query.OrderByDescending(x => x.Data.NgayDang).ToList();
                return View(results);
            }

            // =================================================================
            // AJAX: LIKE (Xử lý lỗi mã 5 ký tự)
            // =================================================================
            [HttpPost]
            public IActionResult ToggleLike(string maTL)
            {
                string maND = "SV001";
                var existingLike = _context.DanhGia.FirstOrDefault(d => d.MaTl == maTL && d.MaNd == maND);

                if (existingLike != null)
                {
                    _context.DanhGia.Remove(existingLike);
                    _context.SaveChanges();
                    return Json(new { success = true, liked = false, count = _context.DanhGia.Count(d => d.MaTl == maTL) });
                }
                else
                {
                    var newLike = new DanhGia
                    {
                        // Mã 5 ký tự: "D" + 4 số cuối của Ticks để tránh lỗi Database
                        MaDg = "D" + System.DateTime.Now.Ticks.ToString().Substring(14, 4),
                        MaTl = maTL,
                        MaNd = maND,
                        SoSaoDg = 5,
                        ThoiGian = System.DateTime.Now
                    };
                    _context.DanhGia.Add(newLike);
                    _context.SaveChanges();
                    return Json(new { success = true, liked = true, count = _context.DanhGia.Count(d => d.MaTl == maTL) });
                }
            }

            [HttpGet]
            public IActionResult GetNganhsByKhoa(string maKhoa)
            {
                var nganhs = _context.Nganhs.Where(n => n.MaKhoa == maKhoa).Select(n => new { n.MaNganh, n.TenNganh }).ToList();
                return Json(nganhs);
            }

            [HttpGet]
            public IActionResult GetMonHocsByNganh(string maNganh)
            {
                var mons = _context.MonHocs.Where(m => m.MaNganh == maNganh).Select(m => new { m.MaMonHoc, m.TenMonHoc }).ToList();
                return Json(mons);
            }

            [HttpGet]
            public IActionResult GetDiemByLoaiTL(string maLtl)
            {
                var loai = _context.LoaiTaiLieus.Include(l => l.MaDqNavigation).FirstOrDefault(l => l.MaLtl == maLtl);
                var diem = loai?.MaDqNavigation?.DiemTl ?? 0;
                return Json(diem);
            }

            // =================================================================
            // AJAX: COMMENT
            // =================================================================
            [HttpPost]
            public IActionResult AddComment(string maTL, string noiDung)
            {
                if (string.IsNullOrEmpty(noiDung)) return Json(new { success = false });
                var comment = new BinhLuan
                {
                    MaBl = "B" + System.DateTime.Now.Ticks.ToString().Substring(14, 4),
                    MaTl = maTL,
                    MaNd = "SV001",
                    NoiDung = noiDung,
                    ThoiGian = System.DateTime.Now
                };
                _context.BinhLuans.Add(comment);
                _context.SaveChanges();
                return Json(new { success = true, userName = "Khoa", content = noiDung, time = System.DateTime.Now.ToString("HH:mm") });
            }

        // --- Các hàm Upload, Details, Report giữ nguyên ---
        // =================================================================
        // DETAILS: Xem chi tiết tài liệu (Đã hoàn thiện dữ liệu)
        // =================================================================
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Lấy tài liệu kèm theo Môn học, Loại, Bình luận và Đánh giá
            var taiLieu = _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)
                    .ThenInclude(m => m.MaNganhNavigation)
                        .ThenInclude(n => n.MaKhoaNavigation) // Bao gồm cả Khoa/Ngành
                .Include(t => t.MaLoaiTlNavigation)
                .Include(t => t.BinhLuans) // Để hiện số bình luận
                .Include(t => t.DanhGia)   // Để tính sao trung bình
                .FirstOrDefault(m => m.MaTaiLieu == id);

            if (taiLieu == null) return NotFound();

            // Lấy thông tin người đăng (Tên, Ảnh) thông qua TaiKhoan
            var tkDang = _context.TaiKhoans.Include(tk => tk.MaSvNavigation).FirstOrDefault(tk => tk.MaTk == taiLieu.MaNguoiDang);
            ViewBag.NguoiDang = tkDang?.MaSvNavigation; // Truyền Model SinhVien nếu có

            // Lấy tên Khoa, Ngành để hiển thị
            ViewBag.TenKhoa = taiLieu.MaMonHocNavigation?.MaNganhNavigation?.MaKhoaNavigation?.TenKhoa;
            ViewBag.TenNganh = taiLieu.MaMonHocNavigation?.MaNganhNavigation?.TenNganh;

            // Lấy danh sách bình luận (Có kèm tên người bình luận thông qua TaiKhoan -> SinhVien)
            var comments = (from b in _context.BinhLuans
                            join tk in _context.TaiKhoans on b.MaNd equals tk.MaTk
                            join sv in _context.SinhViens on tk.MaSv equals sv.MaSv
                            where b.MaTl == id
                            select new
                            {
                                TenSv = sv.TenSv,
                                NoiDung = b.NoiDung,
                                ThoiGian = b.ThoiGian
                            })
                            .OrderByDescending(x => x.ThoiGian)
                            .ToList();

            ViewBag.Comments = comments;

            return View(taiLieu);
        }

        // =================================================================
        // 2. EDIT (UPDATE): Chỉnh sửa thông tin tài liệu
        // =================================================================
        // GET: Hiển thị form sửa
        public IActionResult Edit(string id)
            {
                if (string.IsNullOrEmpty(id)) return NotFound();

                var taiLieu = _context.TaiLieus.Find(id);
                if (taiLieu == null) return NotFound();

                ViewBag.MaMonHoc = _context.MonHocs.ToList();
                ViewBag.MaLoaiTl = _context.LoaiTaiLieus.ToList();

                return View(taiLieu);
            }

            // POST: Lưu dữ liệu sau khi sửa
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(string id, TaiLieu taiLieu, IFormFile fileUpdate)
            {
                if (id != taiLieu.MaTaiLieu) return NotFound();

                try
                {
                    // Lấy dữ liệu gốc từ DB để giữ lại các thông tin không sửa (như Mã người đăng, Ngày đăng)
                    var existingDoc = _context.TaiLieus.AsNoTracking().FirstOrDefault(t => t.MaTaiLieu == id);

                    if (fileUpdate != null && fileUpdate.Length > 0)
                    {
                        // Xử lý nếu người dùng upload file mới thay thế file cũ
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                        string uniqueFileName = DateTime.Now.ToString("yyyyMMddHHmmss_") + fileUpdate.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpdate.CopyToAsync(fileStream);
                        }
                        taiLieu.DuongDanFile = uniqueFileName;
                        taiLieu.LoaiFile = Path.GetExtension(fileUpdate.FileName).Replace(".", "").ToUpper();
                    }
                    else
                    {
                        // Nếu không upload file mới, giữ nguyên đường dẫn file cũ
                        taiLieu.DuongDanFile = existingDoc.DuongDanFile;
                        taiLieu.LoaiFile = existingDoc.LoaiFile;
                    }

                    // Giữ lại các thông tin hệ thống
                    taiLieu.MaNguoiDang = existingDoc.MaNguoiDang;
                    taiLieu.NgayDang = existingDoc.NgayDang;
                    taiLieu.TrangThaiDuyet = existingDoc.TrangThaiDuyet;
                    taiLieu.LuotTai = existingDoc.LuotTai;

                    _context.Update(taiLieu);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TaiLieus.Any(e => e.MaTaiLieu == taiLieu.MaTaiLieu)) return NotFound();
                    else throw;
                }
            }

            // =================================================================
            // 3. REPORT: Báo cáo vi phạm tài liệu
            // =================================================================
            // GET: Hiển thị form báo cáo
            public IActionResult Report(string id)
            {
                if (string.IsNullOrEmpty(id)) return NotFound();

                var taiLieu = _context.TaiLieus.FirstOrDefault(t => t.MaTaiLieu == id);
                if (taiLieu == null) return NotFound();

                ViewBag.TenTaiLieu = taiLieu.TieuDe;
                ViewBag.MaTaiLieu = id;

                return View();
            }

        // POST: Gửi báo cáo về hệ thống
        [HttpPost]
        [ValidateAntiForgeryToken]
      
        public async Task<IActionResult> Report(BaoCaoViPham baoCao)
        {
            // 1. Sinh mã báo cáo tự động (BCxxx)
            var lastBC = _context.BaoCaoViPhams.OrderByDescending(b => b.MaBaoCao).FirstOrDefault();
            baoCao.MaBaoCao = lastBC != null
                ? "BC" + (int.Parse(lastBC.MaBaoCao.Substring(2)) + 1).ToString("D3")
                : "BC001";

            baoCao.NgayBaoCao = DateTime.Now;
            baoCao.TrangThaiXuLy = "Chờ xử lý";
            baoCao.NguoiBaoCao = "SV001"; // Sau này thay bằng Session

            if (ModelState.IsValid)
            {
                _context.BaoCaoViPhams.Add(baoCao);
                await _context.SaveChangesAsync();

                // QUAN TRỌNG: Sau khi lưu xong, ép nó quay về trang RecentReports (trang 2 cột)
                // chứ không được trả về View() mặc định.
                return RedirectToAction("Details", new { id = baoCao.MaTaiLieu });
            }

            // Nếu dữ liệu bị lỗi (ModelState không hợp lệ), ta cũng phải ép nó quay về RecentReports
            // thay vì return View(baoCao) (vì nó sẽ mở cái form lẻ Report.cshtml)
            return RedirectToAction("RecentReports");
        }
        // =================================================================
        // 4. UPLOAD: Đăng tải tài liệu (THIẾU TRONG CODE CỦA BẠN)
        // =================================================================
        // GET: Hiển thị giao diện đăng tải
        public IActionResult Upload()
        {
            ViewBag.MaLoaiTl = _context.LoaiTaiLieus.ToList();
            ViewBag.Khoas = _context.Khoas.ToList();
            return View();
        }

        // POST: Xử lý lưu file khi bấm nút Đăng tải
        [HttpPost]
        public async Task<IActionResult> Upload(TaiLieu taiLieu, IFormFile fileUpload)
        {
            if (fileUpload != null && fileUpload.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = DateTime.Now.ToString("yyyyMMddHHmmss_") + fileUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileUpload.CopyToAsync(fileStream);
                }

                taiLieu.DuongDanFile = uniqueFileName;
                taiLieu.LoaiFile = Path.GetExtension(fileUpload.FileName).Replace(".", "").ToUpper();
                if (taiLieu.LoaiFile.Length > 10) taiLieu.LoaiFile = taiLieu.LoaiFile.Substring(0, 10);

                taiLieu.TrangThaiDuyet = "Chờ duyệt"; // Chờ admin duyệt
                taiLieu.NgayDang = DateTime.Now;
                taiLieu.LuotTai = 0;
                taiLieu.LanTaiBan = 1;

                // Lấy điểm chuẩn từ CSDL dựa trên Loại tài liệu để đảm bảo chính xác
                var loaiTl = _context.LoaiTaiLieus.Include(l => l.MaDqNavigation).FirstOrDefault(l => l.MaLtl == taiLieu.MaLoaiTl);
                taiLieu.DiemYeuCau = loaiTl?.MaDqNavigation?.DiemTl ?? 0;

                // Sinh mã tự động
                var lastItem = _context.TaiLieus.OrderByDescending(t => t.MaTaiLieu).FirstOrDefault();
                taiLieu.MaTaiLieu = lastItem != null
                    ? "TL" + (int.Parse(lastItem.MaTaiLieu.Substring(2)) + 1).ToString("D3")
                    : "TL001";

                var loaiNguoiDung = HttpContext.Session.GetString("LoaiNguoiDung");
                taiLieu.MaNguoiDang = loaiNguoiDung == "SinhVien" 
                    ? HttpContext.Session.GetString("MaSinhVien")
                    : HttpContext.Session.GetString("MaGiangVien");

                _context.TaiLieus.Add(taiLieu);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.MaMonHoc = _context.MonHocs.ToList();
            ViewBag.MaLoaiTl = _context.LoaiTaiLieus.ToList();
            return View(taiLieu);
        }

        // =================================================================
        // 5. RECENT REPORTS: Danh sách báo cáo (THIẾU TRONG CODE CỦA BẠN)
        // =================================================================
        public IActionResult RecentReports(string id)
        {
            // 1. Lấy danh sách tài liệu cho ô chọn (Dropdown)
            ViewBag.DanhSachTaiLieu = _context.TaiLieus.Where(t => t.TrangThaiDuyet == "Đã duyệt").ToList();

            // 2. Lưu lại ID truyền từ trang Chi tiết sang để View tự động chọn
            ViewBag.SelectedId = id;

            // 3. Lấy toàn bộ lịch sử báo cáo để hiển thị ở bảng bên phải
            var lichSu = _context.BaoCaoViPhams
                .Include(b => b.MaTaiLieuNavigation)
                .OrderByDescending(b => b.NgayBaoCao)
                .ToList();

            return View(lichSu);
        }

        // GET: Document/MyDocuments
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

        // GET: Document/GetMyUploads - Lấy tài liệu đã đăng
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
                    .Include(tl => tl.MaLoaiTlNavigation)
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
                        loaiTaiLieu = tl.MaLoaiTlNavigation != null ? tl.MaLoaiTlNavigation.TenLtl : null,
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

        // GET: Document/GetMySaved - Lấy tài liệu đã lưu
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

                var taiLieuDaLuu = await _context.TLYeuThiches
                    .Include(tlt => tlt.MaTlNavigation)
                        .ThenInclude(tl => tl.MaMonHocNavigation)
                    .Include(tlt => tlt.MaTlNavigation)
                        .ThenInclude(tl => tl.MaLoaiTlNavigation)
                    .Where(tlt => tlt.MaNd == maNguoiDung)
                    .OrderByDescending(tlt => tlt.ThoiGian)
                    .Select(tlt => new
                    {
                        maYeuThich = tlt.MaYeuThich,
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
                            loaiTaiLieu = tlt.MaTlNavigation.MaLoaiTlNavigation != null ? tlt.MaTlNavigation.MaLoaiTlNavigation.TenLtl : null,
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

        // GET: Document/GetMyDownloads - Lấy lịch sử tải xuống
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
                        .ThenInclude(tl => tl.MaLoaiTlNavigation)
                    .Where(ls => ls.MaNd == maNguoiDung)
                    .OrderByDescending(ls => ls.NgayTai)
                    .Select(ls => new
                    {
                        maDownTl = ls.MaDownTL,
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
                            loaiTaiLieu = ls.MaTaiLieuNavigation.MaLoaiTlNavigation != null ? ls.MaTaiLieuNavigation.MaLoaiTlNavigation.TenLtl : null,
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