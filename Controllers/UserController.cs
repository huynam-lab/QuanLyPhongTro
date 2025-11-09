using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyPhongTro.Models.ViewModels;
using System.Data.Entity;
using System.IO;                


namespace QuanLyPhongTro.Controllers
{
    public class UserController : Controller
    {
        // Khởi tạo DbContext. Sử dụng DaTa_Phong_TroEntities1 dựa trên connection string bạn cung cấp
        private DaTa_Phong_TroEntities5 db = new DaTa_Phong_TroEntities5();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Lấy danh sách Khu_Vuc có Trang_Thai = true (hoặc logic lọc phù hợp)
            // và sắp xếp theo thứ tự mong muốn
            var khuVucList = db.Khu_Vuc
                               .Where(kv => kv.Trang_Thai == true) // Giả sử Trang_Thai là bool hoặc phù hợp
                               .OrderBy(kv => kv.Ten_KV) // Sắp xếp theo tên khu vực
                               .ToList();

            // Gửi dữ liệu qua ViewBag để Layout có thể truy cập
            ViewBag.KhuVuc = khuVucList;

            base.OnActionExecuting(filterContext);
        }
        // GET: User
        public ActionResult Index(int page = 1, int pageSize = 10)
        {
            var q = db.Phong_Tro
                .AsNoTracking()
                .Include(p => p.Khu_Vuc)
                .Include(p => p.Loai_Tin)
                .Include(p => p.Hinh_Anh)
                .Include(p => p.Tai_Khoan);   // <-- QUAN TRỌNG

            var total = q.Count();

            // Lấy thô từ DB
            var raw = q.OrderByDescending(p => p.Ngay_Dang)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(p => new
              {
                  Id = p.ID_Phong_Tro,
                  Ten = p.Ten_Phong,
                  Gia = p.Gia_Ca,
                  DienTich = p.Dien_Tich,
                  DiaChi = p.Dia_Chi,
                  KhuVuc = p.Khu_Vuc.Ten_KV,

                  // ---- CHỈ DÙNG CÁC CỘT CÓ THẬT ----
                  LoaiTinTen = p.Loai_Tin.Ten_LoaiTin,
                  LoaiTinCapDo = p.Loai_Tin.CapDo,
                  LoaiTinMau = p.Loai_Tin.Mau_TieuDe,   // có trong DB
                                                        // LoaiTinKieuChu = p.Loai_Tin.Kieu_Chu, // <-- XÓA DÒNG NÀY

                  UrlAnhDaiDien = p.Hinh_Anh.Select(h => h.Url_Anh).FirstOrDefault(),
                  UrlAlbum = p.Hinh_Anh.Select(h => h.Url_Anh).ToList(),
                  NgayDang = p.Ngay_Dang,

                  // tuỳ bạn có hay không:
                  ChuNha = p.Tai_Khoan.Name,
                  AvatarUrl = p.Tai_Khoan.Avata,
                  SoDienThoai = p.Tai_Khoan.SDT,
                  MoTaTomTat = p.Mo_Ta
              })
              .ToList();

            var data = raw.Select(p => new PhongTroVM
            {
                Id = p.Id,
                Ten = p.Ten,
                Gia = p.Gia,
                DienTich = p.DienTich,
                DiaChi = p.DiaChi,
                KhuVuc = p.KhuVuc,

                LoaiTinTen = p.LoaiTinTen,
                LoaiTinCapDo = p.LoaiTinCapDo ?? 0,
                LoaiTinMau = string.IsNullOrWhiteSpace(p.LoaiTinMau) ? "#e03" : p.LoaiTinMau,
                // LoaiTinKieuChu = ...  // KHÔNG dùng nữa

                AnhDaiDien = BuildKhoImgWithWard(p.UrlAnhDaiDien),
                Album = (p.UrlAlbum ?? new List<string>()).Select(BuildKhoImgWithWard).Where(s => !string.IsNullOrWhiteSpace(s)).Take(8).ToList(),
                NgayDang = p.NgayDang,

                ChuNha = p.ChuNha,
                AvatarUrl = BuildKhoImgWithWard(p.AvatarUrl),
                SoDienThoai = p.SoDienThoai,
                MoTaTomTat = p.MoTaTomTat
                }).ToList();
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Total = total;
                return View(data);
        }

        // Giữ nguyên subfolder phường nếu DB có: "Phuong_X/abc.jpg"
        // Nếu DB chỉ là "abc.jpg" vẫn hoạt động (không có phường)
        private string BuildKhoImgWithWard(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return raw;
            if (raw.StartsWith("~/")) return raw;

            var sub = raw.Replace('\\', '/').TrimStart('~', '/');
            var path = "~/Kho/Img/" + sub;

            try
            {
                var physical = Server.MapPath(path);
                if (!System.IO.File.Exists(physical))
                    return "~/Assets/Images/no-image.png";
            }
            catch { /* ignore */ }

            return path;
        }
        private string ToYoutubeEmbed(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            // https://youtu.be/ID  hoặc https://www.youtube.com/watch?v=ID
            var m = System.Text.RegularExpressions.Regex.Match(url,
                @"(?:youtu\.be/|v=)(?<id>[A-Za-z0-9_\-]{6,})");
            return m.Success ? $"https://www.youtube.com/embed/{m.Groups["id"].Value}" : url;
        }
        private IEnumerable<QuanLyPhongTro.Models.ViewModels.PhongTroVM>
        QueryPhongTroByChuDe(int idCD, int page, int pageSize, out int total)
        {
            var q = db.Phong_Tro
                .AsNoTracking()
                .Include(p => p.Khu_Vuc)
                .Include(p => p.Loai_Tin)
                .Include(p => p.Hinh_Anh)
                .Where(p => p.ID_CD == idCD);

            total = q.Count();

            var raw = q.OrderByDescending(p => p.Ngay_Dang)
                       .Skip((page - 1) * pageSize)
                       .Take(pageSize)
                       .Select(p => new
                       {
                           Id = p.ID_Phong_Tro,
                           Ten = p.Ten_Phong,
                           Gia = p.Gia_Ca,
                           DienTich = p.Dien_Tich,
                           DiaChi = p.Dia_Chi,
                           KhuVuc = p.Khu_Vuc.Ten_KV,
                           LoaiTin = p.Loai_Tin.Ten_LoaiTin,
                           UrlAnh = p.Hinh_Anh.Select(h => h.Url_Anh).FirstOrDefault(),
                           NgayDang = p.Ngay_Dang
                       })
                       .ToList();

            var data = raw.Select(p => new QuanLyPhongTro.Models.ViewModels.PhongTroVM
            {
                Id = p.Id,
                Ten = p.Ten,
                Gia = p.Gia,
                DienTich = p.DienTich,
                DiaChi = p.DiaChi,
                KhuVuc = p.KhuVuc,
                LoaiTin = p.LoaiTin,
                AnhDaiDien = BuildKhoImgWithWard(p.UrlAnh),
                NgayDang = p.NgayDang
            });

            return data;
        }

        public ActionResult CanHoChungCu(int page = 1, int pageSize = 10)
        {
            int total;
            var model = QueryPhongTroByChuDe(idCD: 6, page: page, pageSize: pageSize, out total);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.ActiveTab = "chungcu"; // để tô màu tab

            return View(model.ToList());
        }

        public ActionResult CanHoMini(int page = 1, int pageSize = 10)
        {
            int total;
            var model = QueryPhongTroByChuDe(idCD: 5, page: page, pageSize: pageSize, out total);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.ActiveTab = "mini";

            return View(model.ToList());
        }
        public ActionResult ChiTiet(int id)
        {
            // lấy 1 phòng theo id + navigation cần thiết
            var vm = db.Phong_Tro
         .Where(x => x.ID_Phong_Tro == id)
         .Select(x => new PhongTroDetailVM
         {
             Id = x.ID_Phong_Tro,
             Ten = x.Ten_Phong,
             Gia = x.Gia_Ca,
             DienTich = x.Dien_Tich,
             DiaChi = x.Dia_Chi,
             KhuVuc = x.Khu_Vuc.Ten_KV,                 // chỉ lấy Ten_KV
             LoaiTinTen = x.Loai_Tin.Ten_LoaiTin,       // chỉ lấy 3 cột này
             LoaiTinCapDo = x.Loai_Tin.CapDo,
             LoaiTinMau = x.Loai_Tin.Mau_TieuDe,

             // ảnh: lấy đúng Url_Anh
             Album = x.Hinh_Anh
                     .OrderBy(h => h.ID_Hinh_Anh)
                     .Select(h => h.Url_Anh)
                     .ToList(),
         })
         .FirstOrDefault();

            if (vm == null) return HttpNotFound();
            return View(vm);
        }
        // =============== start blog
        public ActionResult Blog()
        {
            return View();
        }
        public ActionResult TinTuc()
        {
            return View();
        }
        public ActionResult ChiaSeKinhNghiem()
        {
            return View();
        }
        public ActionResult HoiDap()
        {
            return View();
        }
        public ActionResult MauHopDong()
        {
            return View();
        }
        // =============== end blog
        public ActionResult BangGiaDichVu()
        {
            return View();
        }
        public ActionResult TinDaLuu()
        {
            return View();
        }
        public ActionResult DangKy()
        {
            return View();
        }
        public ActionResult DangNhap()
        {
            return View();
        }
        public ActionResult DangTin()
        {
            return View();
        }
        //  =============== start chi tiet tin
        public ActionResult ChiTietTinTuc(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult ChiTietChiaSeKinhNghiem(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult ChiTietHoiDap(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult ChiTietMauHopDong(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        //  =============== end chi tiet tin
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
