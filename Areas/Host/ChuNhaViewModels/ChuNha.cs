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


        // đặc điểm nổi bật
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

}