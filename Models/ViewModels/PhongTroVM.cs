using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class PhongTroVM
    {
        public int Id { get; set; }
        public string Ten { get; set; }
        public decimal? Gia { get; set; }     // <= decimal? để tính tiền
        public double? DienTich { get; set; } // <= double? cho m2
        public string DiaChi { get; set; }
        public string KhuVuc { get; set; }
        public string TinhThanh { get; set; }
        public DateTime? NgayDang { get; set; }

        public string ChuNha { get; set; }
        public string AvatarUrl { get; set; }
        public string SoDienThoai { get; set; }
        public string LoaiTin { get; set; }

        public int? LoaiTinCapDo { get; set; }
        public string LoaiTinTen { get; set; }
        public string LoaiTinMau { get; set; }
        public string LoaiTinKieuChu { get; set; }

        public string AnhDaiDien { get; set; }
        public List<string> Album { get; set; }
        public string MoTaTomTat { get; set; }
    }
}