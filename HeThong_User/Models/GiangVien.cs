using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class GiangVien
{
    public string MaGv { get; set; } = null!;

    public string TenGv { get; set; } = null!;

    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? Email { get; set; }

    public string? Sdt { get; set; }

    public string? HocVi { get; set; }

    public string? MaKhoa { get; set; }

    public string? MaTk { get; set; }

    public virtual Khoa? MaKhoaNavigation { get; set; }

    public virtual TaiKhoan? MaTkNavigation { get; set; }
}
