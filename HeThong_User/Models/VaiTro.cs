using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class VaiTro
{
    public string MaVaiTro { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
