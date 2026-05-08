using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class LichSuTaiXuong
{
    public int MaDownTL { get; set; }

    public string? MaTaiLieu { get; set; }

    public DateTime? NgayTai { get; set; }

    public string? MaNd { get; set; }

    public virtual TaiKhoan? MaNdNavigation { get; set; }

    public virtual TaiLieu? MaTaiLieuNavigation { get; set; }
}
