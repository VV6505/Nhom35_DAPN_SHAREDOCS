using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class BinhLuan
{
    public string MaBl { get; set; } = null!;

    public string? MaTl { get; set; }

    public string? MaNd { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual TaiKhoan? MaNdNavigation { get; set; }

    public virtual TaiLieu? MaTlNavigation { get; set; }
}
