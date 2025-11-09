using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class PhongTroDetailVM
    {
        // Thông tin chính
        public int Id { get; set; }
        public string Ten { get; set; }
        public decimal? Gia { get; set; }        // Giá: decimal?
        public double? DienTich { get; set; }    // Diện tích: double?
        public string DiaChi { get; set; }
        public string KhuVuc { get; set; }       // Khu_Vuc.Ten_KV
        public string TinhThanh { get; set; }    // nếu bạn có cột, không có thì để trống
        public DateTime? NgayDang { get; set; }
        public string MoTa { get; set; }

        // Loại tin (DB có: Ten_LoaiTin, CapDo, Mau_TieuDe, KichThuoc…)
        public string LoaiTinTen { get; set; }
        public int? LoaiTinCapDo { get; set; }
        public string LoaiTinMau { get; set; }   // dùng Mau_TieuDe làm màu tiêu đề

        // Chủ nhà
        public string ChuNha { get; set; }
        public string SoDienThoai { get; set; }

        // Nổi bật/tiện ích (Nếu muốn hiển thị tick)
        public bool? DayDuNoiThat { get; set; }
        public bool? CoGac { get; set; }
        public bool? CoMayLanh { get; set; }
        public bool? CoMayGiat { get; set; }
        public bool? GioGiacTuDo { get; set; }

        // Media
        public string AnhDaiDien { get; set; }           // ảnh đầu tiên
        public List<string> Album { get; set; } = new List<string>();
        public List<string> Videos { get; set; } = new List<string>(); // Url_Video

        // Liên quan
        
    }

    public class PhongTroShortVM
    {
        public int Id { get; set; }
        public string Ten { get; set; }
        public decimal? Gia { get; set; }
        public double? DienTich { get; set; }
        public string KhuVuc { get; set; }
        public string Anh { get; set; }
    }
}