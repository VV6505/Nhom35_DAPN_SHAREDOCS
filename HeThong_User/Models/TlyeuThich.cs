using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class TLYeuThich
{
    public int MaYeuThich { get; set; }

    public string? MaTl { get; set; }

    public string? MaNd { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual TaiKhoan? MaNdNavigation { get; set; }

    public virtual TaiLieu? MaTlNavigation { get; set; }
}
