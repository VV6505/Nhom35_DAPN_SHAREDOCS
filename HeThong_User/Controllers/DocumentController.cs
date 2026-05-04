using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThong_User.Models;
using System.Linq;

namespace HeThong_User.Controllers
{
    public class DocumentController : Controller
    {
        // Sử dụng đúng tên DbContext của bạn
        private readonly HeThongChiaSeTaiLieu_V1 _context;

        public DocumentController(HeThongChiaSeTaiLieu_V1 context)
        {
            _context = context;
        }

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
    }
}