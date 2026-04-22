using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class LichSuDiem
{
    public string MaLs { get; set; } = null!;

    public string? MaSv { get; set; }

    public int? SoDiemThayDoi { get; set; }

    public string? LyDo { get; set; }

    public DateOnly? NgayThayDoi { get; set; }

    public string? MaHk { get; set; }

    public virtual HocKy? MaHkNavigation { get; set; }

    public virtual SinhVien? MaSvNavigation { get; set; }
}
