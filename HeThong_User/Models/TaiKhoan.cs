using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class TaiKhoan
{
    public string MaTk { get; set; } = null!;

    public string? TenTk { get; set; }

    public string? MatKhau { get; set; }

    public string? MaVaiTro { get; set; }

    public int? TrangThai { get; set; }

    public string? MaGv { get; set; }

    public string? MaSv { get; set; }

    public virtual GiangVien? MaGvNavigation { get; set; }

    public virtual SinhVien? MaSvNavigation { get; set; }

    public virtual VaiTro? MaVaiTroNavigation { get; set; }

    public virtual ICollection<BaoCaoViPham> BaoCaoViPhams { get; set; } = new List<BaoCaoViPham>();

    public virtual ICollection<BinhLuan> BinhLuans { get; set; } = new List<BinhLuan>();

    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    public virtual ICollection<LichSuTaiXuong> LichSuTaiXuongs { get; set; } = new List<LichSuTaiXuong>();

    public virtual ICollection<TaiLieu> TaiLieuMaNguoiDangNavigations { get; set; } = new List<TaiLieu>();

    public virtual ICollection<TaiLieu> TaiLieuMaNguoiDuyetKhoaNavigations { get; set; } = new List<TaiLieu>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<TLYeuThich> TlyeuThiches { get; set; } = new List<TLYeuThich>();
}
