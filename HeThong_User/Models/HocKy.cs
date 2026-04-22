using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class HocKy
{
    public string MaHk { get; set; } = null!;

    public string? TenHk { get; set; }

    public string? NamHoc { get; set; }

    public DateOnly? NgayBd { get; set; }

    public DateOnly? NgayKt { get; set; }

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();
}
