using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class ThongBao
{
    public string MaTb { get; set; } = null!;

    public string? MaNguoiNhan { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDung { get; set; }

    public string? LoaiThongBao { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public string? MaTl { get; set; }

    public virtual TaiKhoan? MaNguoiNhanNavigation { get; set; }

    public virtual TaiLieu? MaTlNavigation { get; set; }
}
