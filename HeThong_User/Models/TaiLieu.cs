using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class TaiLieu
{
    public string MaTaiLieu { get; set; } = null!;

    public string TieuDe { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? DuongDanFile { get; set; }

    public string? LoaiFile { get; set; }

    public double? KichThuoc { get; set; }

    public int? LuotTai { get; set; }

    public bool? CheDoHienThi { get; set; }

    public string? TrangThaiDuyet { get; set; }

    public string? LyDoTuChoi { get; set; }

    public string? MaMonHoc { get; set; }

    public string? MaNguoiDang { get; set; }

    public string? MaBaoCao { get; set; }

    public DateTime? NgayDang { get; set; }

    public int? LanTaiBan { get; set; }

    public string? Nxb { get; set; }

    public DateOnly? NamXb { get; set; }

    public string? MaloaiTl { get; set; }

    public int? DiemYeuCau { get; set; }

    public string? MaNguoiDuyetKhoa { get; set; }

    public virtual ICollection<BaoCaoViPham> BaoCaoViPhams { get; set; } = new List<BaoCaoViPham>();

    public virtual ICollection<BinhLuan> BinhLuans { get; set; } = new List<BinhLuan>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<LichSuTaiXuong> LichSuTaiXuongs { get; set; } = new List<LichSuTaiXuong>();

    public virtual MonHoc? MaMonHocNavigation { get; set; }

    public virtual LoaiTaiLieu? MaloaiTlNavigation { get; set; }

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<TlyeuThich> TlyeuThiches { get; set; } = new List<TlyeuThich>();
}
