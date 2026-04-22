using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class DanhGium
{
    public string MaDg { get; set; } = null!;

    public string? MaTl { get; set; }

    public string? MaNd { get; set; }

    public int? SoSaoDg { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual TaiLieu? MaTlNavigation { get; set; }
}
