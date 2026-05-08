    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using HeThong_User.Models;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using System.IO;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;
    using System.Text;

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

            // =====================================================================
            // HÀM TIỆN ÍCH: Chuẩn hóa chuỗi thành dạng slug (không dấu, không khoảng trắng)
            // VD: "Giáo trình Lập trình Java" -> "GiaoTrinhLapTrinhJava"
            // =====================================================================
            private static string Slugify(string text)
            {
                if (string.IsNullOrEmpty(text)) return "TaiLieu";
                // Bảng chuyển đổi ký tự tiếng Việt sang không dấu
                var vietnameseChars = new Dictionary<string, string>
                {
                    {"à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ", "a"},
                    {"è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ", "e"},
                    {"ì|í|ị|ỉ|ĩ", "i"},
                    {"ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ", "o"},
                    {"ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ", "u"},
                    {"ỳ|ý|ỵ|ỷ|ỹ", "y"},
                    {"đ", "d"},
                    {"À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ", "A"},
                    {"È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ", "E"},
                    {"Ì|Í|Ị|Ỉ|Ĩ", "I"},
                    {"Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ", "O"},
                    {"Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ", "U"},
                    {"Ỳ|Ý|Ỵ|Ỷ|Ỹ", "Y"},
                    {"Đ", "D"}
                };
                foreach (var pair in vietnameseChars)
                    foreach (var ch in pair.Key.Split('|'))
                        text = text.Replace(ch, pair.Value);
                // Chuyển thành PascalCase: viết hoa chữ đầu mỗi từ, bỏ khoảng trắng và ký tự đặc biệt
                var words = Regex.Replace(text, @"[^a-zA-Z0-9\s]", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return string.Concat(words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
            }

            // =====================================================================
            // HÀM TIỆN ÍCH: Sinh tên file chuẩn hóa theo quy tắc đã định
            // Công thức: [MaMonHoc]_[MaLoaiTL]_[TenSlug]_[MaND]_[Nam].[ext]
            // VD: 5169_L0004_SlideJavaSwing_GV001_2025.pdf
            // =====================================================================
            private string GenerateStandardFileName(string maMonHoc, string maLoaiTl, string tieuDe, string maNguoiDang, string extension)
            {
                var mon   = (maMonHoc   ?? "MON").Trim();
                var loai  = (maLoaiTl   ?? "LTL").Trim();
                var slug  = Slugify(tieuDe);
                var nd    = (maNguoiDang ?? "ND").Trim();
                var nam   = DateTime.Now.Year.ToString();
                var ext   = extension.TrimStart('.').ToUpper();
                // Giới hạn slug tối đa 30 ký tự
                if (slug.Length > 30) slug = slug.Substring(0, 30);
                return $"{mon}_{loai}_{slug}_{nd}_{nam}.{ext}";
            }

        // [GET] Trang chủ danh mục: Lấy dữ liệu cây thư mục (Khoa/Ngành/Môn) và danh sách tài liệu đã duyệt để hiển thị.
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
                                TenMon = t.MaMonHocNavigation != null ? t.MaMonHocNavigation.TenMonHoc : ""
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
                    query = query.Where(x => (x.Data.TieuDe != null && x.Data.TieuDe.Contains(searchString)) || (x.Data.MoTa != null && x.Data.MoTa.Contains(searchString)));
                    ViewBag.SearchString = searchString;
                }

                var results = query.OrderByDescending(x => x.Data.NgayDang).ToList();
                return View(results);
            }

            // [POST] Thích tài liệu: Cập nhật trạng thái Thích/Bỏ thích vào Database và trả về số lượt thích mới (AJAX).
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
                    var ticks = System.DateTime.Now.Ticks.ToString();
                    var newLike = new DanhGia
                    {
                        // Mã 5 ký tự: "D" + 4 ký tự cuối của Ticks
                        MaDg = "D" + (ticks.Length > 4 ? ticks.Substring(ticks.Length - 4) : ticks.PadLeft(4, '0')),
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

            // [POST] Thêm bình luận: Lưu nội dung bình luận mới của người dùng vào Database (AJAX).
            [HttpPost]
            public IActionResult AddComment(string maTL, string noiDung)
            {
                if (string.IsNullOrEmpty(noiDung)) return Json(new { success = false });
                var ticksBl = System.DateTime.Now.Ticks.ToString();
                var comment = new BinhLuan
                {
                    MaBl = "B" + (ticksBl.Length > 4 ? ticksBl.Substring(ticksBl.Length - 4) : ticksBl.PadLeft(4, '0')),
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
        // [GET] Chi tiết tài liệu: Lấy toàn bộ thông tin chi tiết của một tài liệu, bao gồm người đăng, bình luận và đánh giá.
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
                
                ViewBag.TenTaiLieu = taiLieu.TieuDe ?? "Không có tiêu đề";
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
            baoCao.NguoiBaoCao = HttpContext.Session.GetString("MaTaiKhoan") ?? "SV001";

            if (ModelState.IsValid)
            {
                _context.BaoCaoViPhams.Add(baoCao);

                // TẠO THÔNG BÁO CHO ADMIN
                var lastTB_RP = _context.ThongBaos.OrderByDescending(t => t.MaTb).FirstOrDefault();
                var nextTB_RP = lastTB_RP != null 
                    ? "TB" + (int.Parse(lastTB_RP.MaTb.Substring(2)) + 1).ToString("D3") 
                    : "TB001";

                var thongBao = new ThongBao
                {
                    MaTb = nextTB_RP,
                    TieuDe = "Báo cáo vi phạm",
                    NoiDung = $"CẢNH BÁO: Tài liệu '{baoCao.MaTaiLieu}' bị báo cáo vi phạm!",
                    NgayTao = DateTime.Now,
                    TrangThai = "Chưa đọc",
                    MaNguoiNhan = "ADMIN"
                };
                _context.ThongBaos.Add(thongBao);

                await _context.SaveChangesAsync();
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

        // [GET] Tải tài liệu (Logic trọng tâm): Kiểm tra điểm tích lũy, thực hiện trừ điểm, ghi lịch sử và trả file về máy khách.
        public async Task<IActionResult> Download(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // BƯỚC 1: Kiểm tra quyền truy cập - Phải đăng nhập mới được tải
            var maSV = HttpContext.Session.GetString("MaSinhVien");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tải tài liệu!";
                return RedirectToAction("Login", "Auth");
            }

            // BƯỚC 2: Lấy thông tin tài liệu từ DB (bao gồm cả loại tài liệu để biết điểm)
            var taiLieu = await _context.TaiLieus.Include(t => t.MaLoaiTlNavigation).FirstOrDefaultAsync(t => t.MaTaiLieu == id);
            var sinhVien = await _context.SinhViens.FindAsync(maSV);

            if (taiLieu == null || sinhVien == null) return NotFound();

            // BƯỚC 3: Kiểm tra quyền sở hữu và vai trò
            // - Giảng viên được tải MIỄN PHÍ.
            // - Chủ bài đăng được tải MIỄN PHÍ.
            var loaiNguoiDung = HttpContext.Session.GetString("LoaiNguoiDung");
            bool isOwner = taiLieu.MaNguoiDang == HttpContext.Session.GetString("MaTaiKhoan");
            bool isLecturer = loaiNguoiDung == "GiangVien";

            if (!isOwner && !isLecturer && (taiLieu.DiemYeuCau ?? 0) > 0)
            {
                // BƯỚC 4: Kiểm tra số dư điểm (Chỉ áp dụng cho Sinh viên)
                if ((sinhVien.DiemTichLuy ?? 0) < (taiLieu.DiemYeuCau ?? 0))
                {
                    TempData["ErrorMessage"] = $"Bạn không đủ điểm để tải tài liệu này! (Cần {taiLieu.DiemYeuCau} điểm)";
                    return RedirectToAction("Details", new { id = id });
                }

                // BƯỚC 5: Thực hiện trừ điểm trực tiếp vào tài khoản sinh viên
                sinhVien.DiemTichLuy -= taiLieu.DiemYeuCau;

                // BƯỚC 6: Lưu vết thay đổi điểm vào bảng lịch sử (LichSuDiem)
                var lichSuDiem = new LichSuDiem
                {
                    // MaLS là int tự tăng
                    MaSv = maSV,
                    SoDiemThayDoi = -(taiLieu.DiemYeuCau ?? 0),
                    LyDo = $"Tải tài liệu: {taiLieu.TieuDe}",
                    NgayThayDoi = DateTime.Now
                };
                _context.LichSuDiems.Add(lichSuDiem);
                
                // BƯỚC 7: Cập nhật lại Session để Frontend đồng bộ hiển thị điểm mới
                HttpContext.Session.SetString("DiemTichLuy", sinhVien.DiemTichLuy?.ToString() ?? "0");
            }

            // BƯỚC 8: Ghi lại lịch sử tải xuống để thống kê
            var lichSuTai = new LichSuTaiXuong
            {
                // MaDownTL là int tự tăng
                MaTaiLieu = id,
                MaNd = HttpContext.Session.GetString("MaTaiKhoan"),
                NgayTai = DateTime.Now
            };
            _context.LichSuTaiXuongs.Add(lichSuTai);

            // BƯỚC 9: Tăng số lượt tải của tài liệu lên 1 đơn vị
            taiLieu.LuotTai = (taiLieu.LuotTai ?? 0) + 1;

            // BƯỚC 10: Lưu toàn bộ thay đổi vào Database
            await _context.SaveChangesAsync();

            // BƯỚC 11: Tìm đường dẫn file vật lý trên server và trả về cho trình duyệt tải xuống
            string filePath = Path.Combine(_env.WebRootPath, "uploads", taiLieu.DuongDanFile ?? "");
            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Tệp tin không tồn tại trên hệ thống!";
                return RedirectToAction("Details", new { id = id });
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", taiLieu.DuongDanFile);
        }

        // POST: Xử lý lưu file khi bấm nút Đăng tải
        [HttpPost]
        public async Task<IActionResult> Upload(TaiLieu taiLieu, IFormFile fileUpload)
        {
            // ===================== VALIDATE TIÊU ĐỀ =====================
            // Quy tắc: [Tên môn học] - [Loại tài liệu] - [Mô tả ngắn]
            // Ví dụ: "Cấu trúc dữ liệu - Giáo trình - Chương 1 đến 5"
            // Ràng buộc: Tối thiểu 10 ký tự, có ít nhất 1 dấu gạch ngang " - "
            if (string.IsNullOrWhiteSpace(taiLieu.TieuDe) || taiLieu.TieuDe.Trim().Length < 10)
            {
                TempData["ErrorMessage"] = "Tiêu đề quá ngắn! Vui lòng nhập ít nhất 10 ký tự.";
                ViewBag.MaLoaiTl = _context.LoaiTaiLieus.ToList();
                ViewBag.Khoas = _context.Khoas.ToList();
                return View(taiLieu);
            }

            if (!taiLieu.TieuDe.Contains(" - "))
            {
                TempData["ErrorMessage"] = "Tiêu đề phải theo định dạng: [Tên môn] - [Loại tài liệu] - [Mô tả]. Ví dụ: \"Cấu trúc dữ liệu - Giáo trình - Chương 1 đến 5\"";
                ViewBag.MaLoaiTl = _context.LoaiTaiLieus.ToList();
                ViewBag.Khoas = _context.Khoas.ToList();
                return View(taiLieu);
            }

            // BƯỚC 0: Kiểm tra trùng tài liệu (Cùng tiêu đề + Cùng môn học)
            var isDuplicate = await _context.TaiLieus.AnyAsync(t => t.TieuDe == taiLieu.TieuDe && t.MaMonHoc == taiLieu.MaMonHoc);
            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "Tài liệu này đã tồn tại trong danh mục của môn học này!";
                ViewBag.MaMonHoc = _context.MonHocs.ToList();
                ViewBag.MaLoaiTl = _context.LoaiTaiLieus.ToList();
                ViewBag.Khoas = _context.Khoas.ToList();
                return View(taiLieu);
            }

            if (fileUpload != null && fileUpload.Length > 0)
            {
                // BƯỚC 1: Xử lý thư mục lưu trữ - Đảm bảo thư mục 'uploads' tồn tại
                string webRootPath = _env.WebRootPath ?? "";
                string uploadsFolder = Path.Combine(webRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // BƯỚC 2: Tạo tên file chuẩn hóa theo quy tắc SHAREDOCS
                // Công thức: [MaMonHoc]_[MaLoaiTL]_[TenSlug]_[MaND]_[Nam].[ext]
                var loaiNguoiDungPre = HttpContext.Session.GetString("LoaiNguoiDung");
                var maNDPre = loaiNguoiDungPre == "SinhVien"
                    ? HttpContext.Session.GetString("MaSinhVien")
                    : HttpContext.Session.GetString("MaGiangVien");
                string fileExtension = Path.GetExtension(fileUpload.FileName); // .pdf / .docx
                string standardFileName = GenerateStandardFileName(
                    taiLieu.MaMonHoc,
                    taiLieu.MaLoaiTl,
                    taiLieu.TieuDe,
                    maNDPre,
                    fileExtension
                );
                // Nếu tên file đã tồn tại (trùng) thì thêm timestamp vào cuối để tránh ghi đè
                string filePath = Path.Combine(uploadsFolder, standardFileName);
                if (System.IO.File.Exists(filePath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(standardFileName);
                    string ext = Path.GetExtension(standardFileName);
                    standardFileName = $"{nameWithoutExt}_{DateTime.Now:HHmmss}{ext}";
                    filePath = Path.Combine(uploadsFolder, standardFileName);
                }

                // BƯỚC 3: Lưu file vật lý vào thư mục trên Server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileUpload.CopyToAsync(fileStream);
                }

                // BƯỚC 4: Gán các thông tin kỹ thuật - dùng standardFileName thay vì uniqueFileName
                taiLieu.DuongDanFile = standardFileName;
                taiLieu.LoaiFile = Path.GetExtension(fileUpload.FileName).Replace(".", "").ToUpper();
                if (taiLieu.LoaiFile?.Length > 10) taiLieu.LoaiFile = taiLieu.LoaiFile.Substring(0, 10);

                // BƯỚC 5: Thiết lập trạng thái mặc định là "Chờ duyệt" để quản trị viên kiểm tra
                taiLieu.TrangThaiDuyet = "Chờ duyệt"; 
                taiLieu.NgayDang = DateTime.Now;
                taiLieu.LuotTai = 0;
                taiLieu.LanTaiBan = 1;

                // BƯỚC 6: Tự động lấy điểm chuẩn từ bảng Loại tài liệu để đảm bảo tính công bằng
                var loaiTl = _context.LoaiTaiLieus.Include(l => l.MaDqNavigation).FirstOrDefault(l => l.MaLtl == taiLieu.MaLoaiTl);
                taiLieu.DiemYeuCau = loaiTl?.MaDqNavigation?.DiemTl ?? 0;

                // BƯỚC 7: Sinh mã tài liệu tự động (Ví dụ: TL001, TL002...)
                var lastItem = _context.TaiLieus.OrderByDescending(t => t.MaTaiLieu).FirstOrDefault();
                taiLieu.MaTaiLieu = lastItem != null
                    ? "TL" + (int.Parse(lastItem.MaTaiLieu.Substring(2)) + 1).ToString("D3")
                    : "TL001";

                // BƯỚC 8: Xác định người đăng dựa trên thông tin Session hiện tại
                var loaiNguoiDung = HttpContext.Session.GetString("LoaiNguoiDung");
                taiLieu.MaNguoiDang = loaiNguoiDung == "SinhVien" 
                    ? HttpContext.Session.GetString("MaSinhVien")
                    : HttpContext.Session.GetString("MaGiangVien");

                // BƯỚC 9: Lưu toàn bộ thông tin vào Database
                _context.TaiLieus.Add(taiLieu);

                // BƯỚC 10: TẠO THÔNG BÁO CHO ADMIN/CBK
                var lastTB = _context.ThongBaos.OrderByDescending(t => t.MaTb).FirstOrDefault();
                var nextTB = lastTB != null 
                    ? "TB" + (int.Parse(lastTB.MaTb.Substring(2)) + 1).ToString("D3") 
                    : "TB001";

                var thongBao = new ThongBao
                {
                    MaTb = nextTB,
                    TieuDe = "Tài liệu mới",
                    NoiDung = $"Có tài liệu mới '{taiLieu.TieuDe}' đang chờ duyệt.",
                    NgayTao = DateTime.Now,
                    TrangThai = "Chưa đọc",
                    MaNguoiNhan = "ADMIN"
                };
                _context.ThongBaos.Add(thongBao);

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // Nếu upload thất bại, nạp lại dữ liệu cho các Dropdown để hiển thị lại form
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