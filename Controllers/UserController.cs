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
        private DaTa_Phong_TroEntities2 db = new DaTa_Phong_TroEntities2();
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
                .Include(p => p.Hinh_Anh);

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
                LoaiTin = p.Loai_Tin.Ten_LoaiTin,
                UrlAnh = p.Hinh_Anh.Select(h => h.Url_Anh).FirstOrDefault(), // CHỈ LẤY STRING
                NgayDang = p.Ngay_Dang
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
                LoaiTin = p.LoaiTin,
                AnhDaiDien = BuildKhoImgWithWard(p.UrlAnh), // OK vì đang chạy trên RAM
                NgayDang = p.NgayDang
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

            // http(s) hoặc "~/..." thì trả nguyên
            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return raw;
            if (raw.StartsWith("~/")) return raw;

            // chuẩn hóa, giữ nguyên subfolder phường
            var subpath = raw.Replace('\\', '/').TrimStart('~', '/');   // vd "Phuong_Ba_Dinh/109_1372.jpg"
            var path = "~/Kho/Img/" + subpath;                         // ==> "~/Kho/Img/Phuong_Ba_Dinh/109_1372.jpg"

            // (tuỳ chọn) nếu muốn fallback no-image khi file không có:
            try
            {
                var physical = Server.MapPath(path);
                if (!System.IO.File.Exists(physical))
                    return "~/Assets/Images/no-image.png";
            }
            catch { /* ignore */ }

            return path;
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
