using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Đây là class 'cầu nối' (ViewModel) chứa dữ liệu tổng hợp
    /// để gửi từ Controller sang View Index.cshtml.
    /// Nó chứa thông tin "phẳng" từ nhiều bảng (Phong_Tro, Loai_Tin, Tai_Khoan...).
    /// </summary>
    public class PhongTroItemViewModel
    {
        // Thông tin cơ bản từ bảng Phong_Tro
        public int ID_PhongTro { get; set; }
        public string TieuDe { get; set; }
        public string DienTich { get; set; }   // Ví dụ: "20 m²"
        public DateTime NgayDang { get; set; }

        // Thông tin tổng hợp từ các bảng liên quan
        public string GiaHienThi { get; set; } // Từ Bang_Gia_Tin, ví dụ: "4.5 triệu/tháng"
        public string DiaChiDayDu { get; set; } // Từ Phong_Tro + Khu_Vuc, ví dụ: "Gò Vấp, Hồ Chí Minh"
        public string MoTaTienIch { get; set; } // Từ Noi_Bat, ví dụ: "Tủ lạnh, máy lạnh, gác..."

        // Thông tin từ bảng Hinh_Anh
        public string HinhAnhDauTien { get; set; } // URL ảnh bìa
        public int TongSoHinhAnh { get; set; }

        // Thông tin từ bảng Tai_Khoan
        public string TenNguoiDang { get; set; }
        public string SoDienThoai { get; set; }

        // --- PHẦN NÂNG CẤP CHO 5 CẤP TIN ---
        // Thông tin từ bảng Loai_Tin
        public string TenLoaiTin { get; set; } // Ví dụ: "Tin VIP Nổi Bật", "Tin VIP 1"
        public string MauTieuDe { get; set; }  // Ví dụ: "MÀU ĐỎ, IN HOA" hoặc mã màu "#E03C31"
    }
}