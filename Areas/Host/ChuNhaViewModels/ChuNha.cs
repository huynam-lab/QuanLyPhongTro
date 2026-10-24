using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyPhongTro.Areas.Admin.ChuNhaViewModels
    {
    public class ChuNha
        {

        public IEnumerable<Khu_Vuc> KhuVuc { get; set; }
        public IEnumerable<Loai_Tin> LoaiTin { get; set; }
        public IEnumerable<BangGiaTinDTO> BangGiaTin { get; set; }
        public List<PhongTroListVM> PhongTros { get; set; }
        public string NameChuNha { get; set; }
        public int ID_TK { get; set; }
        public string Avataa { get; set; }
        public string Sdt { get; set; }
        public string TieuDe { get; set; }
        public string MoTa { get; set; }
        public double? GiaThue { get; set; }
        public float? DienTich { get; set; }
        public string DiaChi { get; set; }
        public decimal? GiaCa { get; set; }
        public int? ID_KV { get; set; }
        public int? ID_Phong_Tro { get; set; }
        public int? ID_CD { get; set; }
        public int? ID_LoaiTin { get; set; }
        public int? Ngay { get; set; }
        public decimal? BangGia { get; set; }
        public List<string> HinhAnhs { get; set; }
        public List<string> Videos { get; set; }


        public bool DayDuNoiThat { get; set; }
        public bool CoMayLanh { get; set; }
        public bool CoThangMay { get; set; }
        public bool CoBaoVe { get; set; }
        public bool CoGac { get; set; }
        public bool CoMayGiat { get; set; }
        public bool KhongChungChu { get; set; }
        public bool CoKeBep { get; set; }
        public bool CoTuLanh { get; set; }
        public bool GioGiacTuDo { get; set; }
        }
    public class BangGiaTinDTO
        {
        public int ID_Gia { get; set; }
        public int ID_LoaiTin { get; set; }
        public string Thoi_Gian { get; set; }
        public decimal Gia_Goc { get; set; }
        public decimal Gia_Giam { get; set; }
        public string Ghi_Chu { get; set; }
        }
    public class PhongTroListVM
        {
        public int ID_Phong_Tro { get; set; }
        public string Ten_Phong { get; set; }
        public DateTime? Ngay_Dang { get; set; }
        public bool? Trang_Thai { get; set; }  
        }

    public class HostRoomStatusItem
    {
        public int IdPhong { get; set; }
        public string TenPhong { get; set; }
        public decimal GiaThue { get; set; }
        public string TenKhuVuc { get; set; }
        public bool DaThue { get; set; }
        public DateTime? NgayDang { get; set; }
    }

    public class ChuNhaForecastVM : ChuNha
    {
        public ForecastResult Forecast { get; set; }
        public int TongPhongCuaToi { get; set; }
        public int PhongDaThueCuaToi { get; set; }
        public double TyLeLapDayCuaToi { get; set; }
        public int SelectedMonths { get; set; }
        public int? SelectedIdKv { get; set; }
        public string TenKhuVucHienTai { get; set; }
        public List<HostRoomStatusItem> DanhSachPhongCuaToi { get; set; } = new List<HostRoomStatusItem>();
    }

    public class ThongKeDoanhThuVM : ChuNha
    {
        public decimal TongDoanhThu { get; set; }
        public decimal DaThu { get; set; }
        public decimal ChuaThu { get; set; }
        public decimal TongTienPhong { get; set; }
        public decimal TongTienDien { get; set; }
        public decimal TongTienNuoc { get; set; }
        public decimal TongTienWifi { get; set; }
        public int? SelectedThang { get; set; }
        public int? SelectedNam { get; set; }
        public List<int> DanhSachNam { get; set; }
        public List<DoanhThuThangItem> DoanhThuTheoThang { get; set; }
        public List<HoaDonChiTietItem> DanhSachHoaDon { get; set; }
    }

    public class DoanhThuThangItem
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int SoHoaDon { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDien { get; set; }
        public decimal TienNuoc { get; set; }
        public decimal TienWifi { get; set; }
        public decimal TongCong { get; set; }
        public decimal DaThu { get; set; }
        public decimal ChuaThu { get; set; }
    }

    public class HoaDonChiTietItem
    {
        public int ID_Hoa_Don { get; set; }
        public string TenPhong { get; set; }
        public string TenKhach { get; set; }
        public string SDT { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal SoDien { get; set; }
        public decimal TienDien { get; set; }
        public decimal SoNuoc { get; set; }
        public decimal TienNuoc { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienWifi { get; set; }
        public decimal TongTien { get; set; }
        public DateTime NgayLap { get; set; }
        public DateTime? HanThanhToan { get; set; }
        public bool TrangThai { get; set; }
    }

    public class QuanLyPhongTroVM : ChuNha
    {
        public int TongSoPhong { get; set; }
        public int SoPhongDangThue { get; set; }
        public int SoPhongTrong { get; set; }
        public decimal TongGiaTriThueThang { get; set; }
        public List<PhongTroQuanLyItem> DanhSachPhong { get; set; }
        public List<LichSuThuePhongItem> LichSuThue { get; set; }
    }

    public class LichSuThuePhongItem
    {
        public int ID_Hop_Dong { get; set; }
        public int ID_Phong_Tro { get; set; }
        public string TenPhong { get; set; }
        public string TenKhach { get; set; }
        public string SDT { get; set; }
        public string CCCD { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public decimal GiaThue { get; set; }
        public bool TrangThai { get; set; }
    }

    public class PhongTroQuanLyItem
    {
        public int ID_Phong_Tro { get; set; }
        public string TenPhong { get; set; }
        public string TenKhuVuc { get; set; }
        public string DiaChi { get; set; }
        public double? DienTich { get; set; }
        public decimal? GiaThue { get; set; }
        public DateTime? NgayDang { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public bool TrangThaiDuyet { get; set; }
        public bool DangCoKhachThue { get; set; }
        public string TenKhachThue { get; set; }
        public string SDTKhachThue { get; set; }
        public DateTime? NgayBatDauThue { get; set; }
        public DateTime? NgayKetThucThue { get; set; }
    }

    public class QuanLyKhachThueVM : ChuNha
    {
        public int TongKhach { get; set; }
        public int KhachDangThue { get; set; }
        public int KhachDaKetThuc { get; set; }
        public List<KhachThueItem> DanhSachKhach { get; set; }
    }

    public class KhachThueItem
    {
        public int ID_Khach_Thue { get; set; }
        public string HoTen { get; set; }
        public string CCCD { get; set; }
        public string SDT { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string DiaChi { get; set; }
        public string TenPhong { get; set; }
        public int ID_HopDong { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public decimal GiaThue { get; set; }
        public bool HopDongHieuLuc { get; set; }
    }

    public class QuanLyDienNuocVM : ChuNha
    {
        public decimal TongTienDien { get; set; }
        public decimal TongTienNuoc { get; set; }
        public int SoHoaDonDaThu { get; set; }
        public int SoHoaDonChuaThu { get; set; }
        public decimal TongTienDaThu { get; set; }
        public decimal TongTienChuaThu { get; set; }
        public int? SelectedThang { get; set; }
        public int? SelectedNam { get; set; }
        public bool? SelectedTrangThai { get; set; }
        public List<int> DanhSachNam { get; set; }
        public List<HoaDonChiTietItem> DanhSachHoaDon { get; set; }
    }

    public class QuanLyHopDongVM : ChuNha
    {
        public int TongHopDong { get; set; }
        public int HopDongHieuLuc { get; set; }
        public int HopDongHetHan { get; set; }
        public decimal TongGiaTriThue { get; set; }
        public List<HopDongItem> DanhSachHopDong { get; set; }
    }

    public class HopDongItem
    {
        public int ID_Hop_Dong { get; set; }
        public int ID_Phong_Tro { get; set; }
        public string TenPhong { get; set; }
        public int ID_Khach_Thue { get; set; }
        public string TenKhach { get; set; }
        public string SDT { get; set; }
        public string CCCD { get; set; }
        public DateTime NgayKy { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public decimal GiaThue { get; set; }
        public int KyThanhToan { get; set; }
        public bool TrangThai { get; set; }
    }
}