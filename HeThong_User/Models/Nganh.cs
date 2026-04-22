using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class Nganh
{
    public string MaNganh { get; set; } = null!;

    public string TenNganh { get; set; } = null!;

    public string? MaKhoa { get; set; }

    public virtual Khoa? MaKhoaNavigation { get; set; }

    public virtual ICollection<MonHoc> MonHocs { get; set; } = new List<MonHoc>();
}
