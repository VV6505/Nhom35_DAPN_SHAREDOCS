using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class GiangVien
{
    public string MaGv { get; set; } = null!;

    public string TenGv { get; set; } = null!;

    public string? GioiTinh { get; set; }

    public DateTime? NgaySinh { get; set; }

    public string? Email { get; set; }

    public string? Sdt { get; set; }

    public string? HocVi { get; set; }

    public string LoaiGv { get; set; } = null!;

    public string? MaKhoa { get; set; }

    public virtual Khoa? MaKhoaNavigation { get; set; }

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
