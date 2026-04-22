using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class SinhVien
{
    public string MaSv { get; set; } = null!;

    public string TenSv { get; set; } = null!;

    public string? Email { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public int? DiemTichLuy { get; set; }

    public string? MaLop { get; set; }

    public string? TrangThaiSv { get; set; }

    public string? MaTk { get; set; }

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual Lop? MaLopNavigation { get; set; }

    public virtual TaiKhoan? MaTkNavigation { get; set; }
}
