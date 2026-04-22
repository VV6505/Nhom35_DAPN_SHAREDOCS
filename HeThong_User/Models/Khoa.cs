using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class Khoa
{
    public string MaKhoa { get; set; } = null!;

    public string TenKhoa { get; set; } = null!;

    public virtual ICollection<GiangVien> GiangViens { get; set; } = new List<GiangVien>();

    public virtual ICollection<Lop> Lops { get; set; } = new List<Lop>();

    public virtual ICollection<Nganh> Nganhs { get; set; } = new List<Nganh>();
}
