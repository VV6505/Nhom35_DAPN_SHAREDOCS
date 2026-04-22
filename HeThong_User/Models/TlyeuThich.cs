using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class TlyeuThich
{
    public string MaTll { get; set; } = null!;

    public string? MaTl { get; set; }

    public string? MaNd { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual TaiLieu? MaTlNavigation { get; set; }
}
