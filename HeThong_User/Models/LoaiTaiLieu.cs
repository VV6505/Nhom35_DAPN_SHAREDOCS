using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class LoaiTaiLieu
{
    public string MaLtl { get; set; } = null!;

    public string? TenLtl { get; set; }

    public virtual ICollection<TaiLieu> TaiLieus { get; set; } = new List<TaiLieu>();
}
