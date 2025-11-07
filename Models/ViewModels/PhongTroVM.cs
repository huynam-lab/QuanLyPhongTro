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
        public decimal? Gia { get; set; }
        public double? DienTich { get; set; }
        public string DiaChi { get; set; }
        public string KhuVuc { get; set; }
        public string LoaiTin { get; set; }
        public string AnhDaiDien { get; set; }
        public DateTime? NgayDang { get; set; }
    }
}