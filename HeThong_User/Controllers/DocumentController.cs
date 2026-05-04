using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThong_User.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting; // Dùng để lấy đường dẫn lưu file
using Microsoft.AspNetCore.Http;    // Dùng để nhận file upload
using System.IO;                    // Dùng để xử lý file
using System.Threading.Tasks;

namespace HeThong_User.Controllers
{
    public class DocumentController : Controller
    {
        private readonly HeThongChiaSeTaiLieu_V1 _context;
        private readonly IWebHostEnvironment _env; // Công cụ chỉ đường dẫn lưu file

        // Cập nhật lại hàm khởi tạo (Constructor)
        public DocumentController(HeThongChiaSeTaiLieu_V1 context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =================================================================
        public IActionResult Index(List<string> loaiTaiLieuIds)
        {
            // 1. Lấy danh sách Loại tài liệu từ SQL để tạo Checkbox tự động trên giao diện
            ViewBag.DanhSachLoai = _context.LoaiTaiLieus.ToList();

            // 2. Lưu lại danh sách các mục người dùng đã tick chọn để giữ sáng Checkbox sau khi load lại trang
            ViewBag.SelectedIds = loaiTaiLieuIds ?? new List<string>();

            // 3. Khởi tạo câu truy vấn lấy tài liệu (chưa chạy ngay xuống DB)
            var query = _context.TaiLieus
                .Include(t => t.MaMonHocNavigation)
                .Include(t => t.MaloaiTlNavigation)
                .Where(t => t.TrangThaiDuyet == "DaDuyet")
                .AsQueryable();

            // 4. Nếu người dùng CÓ tick chọn bộ lọc, thì thêm điều kiện lọc vào câu truy vấn
            if (loaiTaiLieuIds != null && loaiTaiLieuIds.Any())
            {
                query = query.Where(t => loaiTaiLieuIds.Contains(t.MaloaiTl));
            }

            // 5. Thực thi truy vấn (.ToList()) và trả kết quả ra View
            var danhSachTaiLieu = query.ToList();
            return View(danhSachTaiLieu);

           
        }
        // =================================================================
       

        // =================================================================
        // 1. GET: Hiển thị giao diện Đăng tải (Form)
        // =================================================================
        public IActionResult Upload()
        {
            // Gửi danh sách Môn Học và Loại Tài Liệu ra View để làm Menu thả xuống (Select Box)
            ViewBag.MaMonHoc = _context.MonHocs.ToList();
            ViewBag.MaloaiTl = _context.LoaiTaiLieus.ToList();
            return View();
        }

        // =================================================================
        // 2. POST: Xử lý dữ liệu khi người dùng bấm nút "Đăng tải"
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> Upload(TaiLieu taiLieu, IFormFile fileUpload)
        {
            // Kiểm tra xem người dùng đã chọn file chưa
            if (fileUpload != null && fileUpload.Length > 0)
            {
                // 1. LƯU FILE VÀO THƯ MỤC WEB (wwwroot/uploads)
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder); // Tạo thư mục nếu chưa có

                // Tạo tên file độc nhất (thêm mã thời gian để không bị trùng đè file cũ)
                string uniqueFileName = System.DateTime.Now.ToString("yyyyMMddHHmmss_") + fileUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileUpload.CopyToAsync(fileStream); // Copy file vào server
                }

                // 2. ĐIỀN THÔNG TIN TỰ ĐỘNG CHO DATABASE
                taiLieu.DuongDanFile = uniqueFileName;

                // Lấy đuôi file (VD: .pdf -> PDF)
                taiLieu.LoaiFile = Path.GetExtension(fileUpload.FileName).Replace(".", "").ToUpper();
                if (taiLieu.LoaiFile.Length > 10) taiLieu.LoaiFile = taiLieu.LoaiFile.Substring(0, 10);

                taiLieu.TrangThaiDuyet = "ChoDuyet"; // Mặc định phải chờ Admin duyệt
                taiLieu.NgayDang = System.DateTime.Now;
                taiLieu.LuotTai = 0;
                taiLieu.LanTaiBan = 1;
                taiLieu.DiemYeuCau = 0; // Mặc định miễn phí

                // 3. Tự động sinh Mã Tài Liệu (Ví dụ: TL010 -> TL011)
                var lastItem = _context.TaiLieus.OrderByDescending(t => t.MaTaiLieu).FirstOrDefault();
                if (lastItem != null)
                {
                    int nextId = int.Parse(lastItem.MaTaiLieu.Substring(2)) + 1;
                    taiLieu.MaTaiLieu = "TL" + nextId.ToString("D3");
                }
                else
                {
                    taiLieu.MaTaiLieu = "TL001";
                }

                // Gán tạm người đăng là Sinh Viên 001 (Sau này có chức năng Login bạn sẽ sửa lại chỗ này)
                taiLieu.MaNguoiDang = "SV001";

                // 4. LƯU VÀO DATABASE
                _context.TaiLieus.Add(taiLieu);
                await _context.SaveChangesAsync();

                // Lưu thành công thì đưa người dùng quay lại trang Danh sách
                return RedirectToAction("Index");
            }

            // Nếu upload lỗi, tải lại trang và báo lỗi
            ViewBag.MaMonHoc = _context.MonHocs.ToList();
            ViewBag.MaloaiTl = _context.LoaiTaiLieus.ToList();
            ModelState.AddModelError("", "Vui lòng chọn một file để tải lên.");
            return View(taiLieu);
        }
    }
}