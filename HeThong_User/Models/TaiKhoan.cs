using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class TaiKhoan
{
    public string MaTk { get; set; } = null!;

    public string TenTk { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? MaVaiTro { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<GiangVien> GiangViens { get; set; } = new List<GiangVien>();

    public virtual VaiTro? MaVaiTroNavigation { get; set; }

    public virtual ICollection<SinhVien> SinhViens { get; set; } = new List<SinhVien>();
}
