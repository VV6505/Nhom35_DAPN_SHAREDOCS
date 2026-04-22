using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class MonHoc
{
    public string MaMonHoc { get; set; } = null!;

    public string TenMonHoc { get; set; } = null!;

    public string? MaNganh { get; set; }

    public virtual Nganh? MaNganhNavigation { get; set; }

    public virtual ICollection<TaiLieu> TaiLieus { get; set; } = new List<TaiLieu>();
}
