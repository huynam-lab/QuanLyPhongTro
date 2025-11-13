using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyPhongTro.Areas.Admin.AdminViewModels
    {
    public class CatalogVM
        {
        public System.Collections.Generic.List<Khu_Vuc> KhuVucs { get; set; }
        public System.Collections.Generic.List<Loai_Tin> LoaiTins { get; set; }
        public System.Collections.Generic.List<Bang_Gia_Tin> BangGias { get; set; }
        public System.Collections.Generic.List<Noi_Bat> NoiBats { get; set; }
        public List<Chu_De> ChuDes { get; set; }

        }
    }