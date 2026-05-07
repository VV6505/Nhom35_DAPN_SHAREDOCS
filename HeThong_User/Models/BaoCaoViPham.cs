using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class BaoCaoViPham
{
    public string MaBaoCao { get; set; } = null!;

    public string? MaTaiLieu { get; set; }

    public string? NguoiBaoCao { get; set; }

    public string? LyDo { get; set; }

    public string? MoTaChiTiet { get; set; }

    public string? TrangThaiXuLy { get; set; }

    public DateTime? NgayBaoCao { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public virtual TaiLieu? MaTaiLieuNavigation { get; set; }
    public virtual TaiKhoan? NguoiBaoCaoNavigation { get; set; }
}
