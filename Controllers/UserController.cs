using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult CanHoChungCu()
        {
            return View();
        }
        public ActionResult CanHoMini()
        {
            return View();
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
