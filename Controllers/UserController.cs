using PagedList; // <-- 1. THÊM THƯ VIỆN NÀY
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

        public ActionResult Index(int? page,int? minPrice, int? maxPrice, int? minArea, int? maxArea)
        {
            int? currentUserId = null;
            if (Session["ID_TK"] != null)
            {
                currentUserId = (int)Session["ID_TK"];
            }

            // ----- LẤY DANH SÁCH YÊU THÍCH CỦA NGƯỜI NÀY -----
            var userFavorites = new List<int>();
            if (currentUserId.HasValue)
            {
                userFavorites = db.Yeu_Thich
                                  .Where(yt => yt.ID_TK == currentUserId.Value)
                                  .Select(yt => yt.ID_Phong_Tro)
                                  .ToList();
            }
            ViewBag.UserFavorites = userFavorites;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentMinArea = minArea;
            ViewBag.CurrentMaxArea = maxArea;
            // ----- PHÂN TRANG -----
            int pageSize = 10; // Số item mỗi trang
            int pageNumber = (page ?? 1); // Trang hiện tại, nếu không có thì là trang 1

            // 3. SỬA CÂU TRUY VẤN
            var query = db.Phong_Tro
                          .Include(p => p.Tai_Khoan)
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Loai_Tin)
                          .Where(p => p.ID_CD == 4); // Lọc theo ID_CD = 4
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Gia_Ca >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Gia_Ca < maxPrice.Value); // Dùng '<' để "dưới 2 triệu" không bao gồm 2 triệu
            }
            if (minArea.HasValue)
            {
                query = query.Where(p => p.Dien_Tich >= minArea.Value);
            }
            if (maxArea.HasValue)
            {
                query = query.Where(p => p.Dien_Tich <= maxArea.Value);
            }
            // SẮP XẾP: Ưu tiên theo Loại Tin (1 -> 5), sau đó mới tới Ngày Đăng
            var sortedQuery = query.OrderBy(p => p.ID_LoaiTin)
                                   .ThenByDescending(p => p.Ngay_Dang);

            // 4. GỌI ToPagedList() THAY VÌ ToList()
            // Gửi Model phân trang qua View
            return View(sortedQuery.ToPagedList(pageNumber, pageSize));
        }
        private List<int> GetUserFavorites()
        {
            if (Session["ID_TK"] != null)
            {
                int currentUserId = (int)Session["ID_TK"];
                return db.Yeu_Thich
                         .Where(yt => yt.ID_TK == currentUserId)
                         .Select(yt => yt.ID_Phong_Tro)
                         .ToList();
            }
            return new List<int>(); // Trả về danh sách rỗng nếu chưa đăng nhập
        }
        [HttpPost]
        public JsonResult ToggleFavorite(int id) // id này là ID_Phong_Tro
        {
            // 1. Kiểm tra xem người dùng đã đăng nhập chưa
            if (Session["ID_TK"] == null)
            {
                // Trả về lỗi 401 (Chưa xác thực)
                return Json(new { success = false, message = "Bạn cần đăng nhập." }, JsonRequestBehavior.AllowGet);
                // Hoặc bạn có thể trả về lỗi 401
                // Response.StatusCode = 401gi;
                // return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            int currentUserId = (int)Session["ID_TK"];
            bool isFavorited = false;

            // 2. Kiểm tra xem tin này đã được yêu thích chưa
            var existingFavorite = db.Yeu_Thich
                                     .FirstOrDefault(yt => yt.ID_Phong_Tro == id && yt.ID_TK == currentUserId);

            if (existingFavorite != null)
            {
                // 3a. ĐÃ CÓ -> Bỏ yêu thích (Xóa khỏi DB)
                db.Yeu_Thich.Remove(existingFavorite);
                isFavorited = false;
            }
            else
            {
                // 3b. CHƯA CÓ -> Thêm yêu thích (Thêm vào DB)
                var newFavorite = new Yeu_Thich
                {
                    ID_Phong_Tro = id,
                    ID_TK = currentUserId
                };
                db.Yeu_Thich.Add(newFavorite);
                isFavorited = true;
            }

            db.SaveChanges();

            // 4. Trả về kết quả (dạng JSON)
            return Json(new { success = true, isFavorited = isFavorited });
        }

        public ActionResult CanHoChungCu(int? page) // Sửa tham số thành int? page
        {
            // Lấy danh sách ID yêu thích
            var userFavorites = GetUserFavorites();
            ViewBag.UserFavorites = userFavorites;

            // Cấu hình phân trang
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            // Truy vấn
            var query = db.Phong_Tro
                          .Include(p => p.Tai_Khoan)
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Loai_Tin)
                          .Where(p => p.ID_CD == 6); // <-- LỌC THEO ID_CD = 6

            var sortedQuery = query.OrderBy(p => p.ID_LoaiTin)
                                   .ThenByDescending(p => p.Ngay_Dang);
            var newestPosts = db.Phong_Tro
                        .Include(p => p.Hinh_Anh) // Chỉ cần include ảnh
                        .OrderByDescending(p => p.Ngay_Dang)
                        .Take(10)
                        .ToList();

            // 3. ĐƯA DANH SÁCH SIDEBAR VÀO VIEWBAG
            ViewBag.NewestPosts = newestPosts;
            // QUAN TRỌNG: Render ra View tên là "Index"

            return View("Index", sortedQuery.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult CanHoMini(int? page) // Sửa tham số thành int? page
        {
            // Lấy danh sách ID yêu thích
            var userFavorites = GetUserFavorites();
            ViewBag.UserFavorites = userFavorites;

            // Cấu hình phân trang
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            // Truy vấn
            var query = db.Phong_Tro
                          .Include(p => p.Tai_Khoan)
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Loai_Tin)
                          .Where(p => p.ID_CD == 5); // <-- LỌC THEO ID_CD = 5

            var sortedQuery = query.OrderBy(p => p.ID_LoaiTin)
                                   .ThenByDescending(p => p.Ngay_Dang);

            // QUAN TRỌNG: Render ra View tên là "Index"
            return View("Index", sortedQuery.ToPagedList(pageNumber, pageSize));
        }
        public ActionResult ChiTiet(int id) // <-- SỬA LỖI: Thêm tham số (int id)
        {
            // Truy vấn CSDL để tìm phòng trọ có ID này
            var phongTro = db.Phong_Tro
                             .Include(p => p.Tai_Khoan)
                             .Include(p => p.Khu_Vuc)
                             .Include(p => p.Hinh_Anh)
                             .Include(p => p.Loai_Tin)
                             .Include(p => p.Noi_Bat)
                             .FirstOrDefault(p => p.ID_Phong_Tro == id);

            // Nếu không tìm thấy phòng trọ, trả về lỗi
            if (phongTro == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách yêu thích (cho nút trái tim)
            var userFavorites = new List<int>();
            if (Session["ID_TK"] != null)
            {
                int currentUserId = (int)Session["ID_TK"];
                userFavorites = db.Yeu_Thich
                                  .Where(yt => yt.ID_TK == currentUserId)
                                  .Select(yt => yt.ID_Phong_Tro)
                                  .ToList();
            }
            ViewBag.UserFavorites = userFavorites;

            // Gửi 1 đối tượng phòng trọ duy nhất qua View
            return View(phongTro);
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
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }
        [HttpPost]
        //[ValidateAntiForgeryToken] // Nên thêm để chống Cross-Site Request Forgery (CSRF)
        public ActionResult DangKy(string Name, string SDT, string Pass, string accountType)
        {

            // 1. KIỂM TRA TRÙNG SĐT (dùng SDT làm User_Name)
            if (db.Tai_Khoan.Any(tk => tk.SDT == SDT))
            {
                ViewBag.HasError = true; // Cờ báo lỗi để JS focus input SDT
                ViewBag.RegName = Name; // Cờ báo lỗi để JS xử lý focus và tô đỏ
                return View();
            }

            // 2. Gán ID_Phan_Quyen (Đoạn này giữ nguyên)
            int idPhanQuyen;
            switch (accountType)
            {
                case "timkiem": idPhanQuyen = 2; break;
                case "chinhchu": idPhanQuyen = 3; break;
                case "admin": idPhanQuyen = 1; break;
                default: idPhanQuyen = 2; break;
            }

            // 3. Tạo đối tượng Tai_Khoan mới (Đoạn này giữ nguyên)
            Tai_Khoan newAccount = new Tai_Khoan();
            newAccount.Name = Name;
            newAccount.SDT = SDT;
            newAccount.User_Name = SDT;
            newAccount.Pass = Utilities.HashPassword(Pass);
            newAccount.ID_Phan_Quyen = idPhanQuyen;
            newAccount.Ngay_Tao = DateTime.Now;
            newAccount.Trang_Thai = true; // Hoặc true tùy theo kiểu dữ liệu của bạn

            try
            {
                // 4. Lưu vào Database
                db.Tai_Khoan.Add(newAccount);
                db.SaveChanges();

                // 5. Đăng ký thành công
                ViewBag.RegistrationSuccess = true;
                ViewBag.RegName = Name;
                return View(); // Trả về View để JS hiển thị Modal
            }
            catch (Exception ex)
            {
                ViewBag.HasError = true;
                ViewBag.RegName = Name;
                return View();
            }
        }
        [HttpGet]
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult DangNhap(string SDT, string Pass) // Đổi tên tham số đầu vào thành SDT
        {
            // --- BƯỚC 0: KIỂM TRA INPUT (Xử lý lỗi thiếu thông tin) ---
            // SDT lúc này chính là User_Name hoặc số điện thoại mà người dùng nhập
            if (string.IsNullOrEmpty(SDT) || string.IsNullOrEmpty(Pass))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Số điện thoại và Mật khẩu.";
                ViewBag.ErrorType = "EmptyFields";
                ViewBag.HasError = true;
                ViewBag.AttemptedUserName = SDT; // Lưu lại SDT đã nhập
                return View();
            }

            // 1. Mã hóa mật khẩu người dùng nhập
            string hashedPassword = Utilities.HashPassword(Pass);

            // Nếu hàm HashPassword trả về null hoặc rỗng, coi là lỗi hệ thống hoặc dữ liệu
            if (string.IsNullOrEmpty(hashedPassword))
            {
                ViewBag.Error = "Lỗi xử lý mật khẩu. Vui lòng thử lại.";
                ViewBag.HasError = true;
                ViewBag.AttemptedUserName = SDT;
                return View();
            }

            // 2. Tìm kiếm tài khoản dựa trên SDT và mật khẩu đã mã hóa
            // Dùng tk.SDT để tra cứu, giả định cột SDT trong Model Tai_Khoan là duy nhất
            var account = db.Tai_Khoan
                            .SingleOrDefault(tk => tk.SDT == SDT && tk.Pass == hashedPassword);
            // HOẶC dùng tk.User_Name nếu bạn xác định User_Name chính là SDT

            // --- Xử lý Logic Đăng Nhập ---
            if (account != null)
            {
                // 3. Đăng nhập THÀNH CÔNG
                
                Session["UserID"] = account.ID_TK;
                Session["UserName"] = account.Name;
                Session["SDT"] = account.SDT;
                Session["Avatar"] = account.Avata;
                ViewBag.ID_Phan_Quyen = account.ID_Phan_Quyen;
                ViewBag.LoginSuccess = true;

                return View(); // Trả về View để JS show Modal
            }
            else
            {
                // 4. Đăng nhập THẤT BẠI
                // Kiểm tra xem SDT có tồn tại trong DB không (dùng cột SDT)
                var checkUser = db.Tai_Khoan.SingleOrDefault(tk => tk.SDT == SDT);

                if (checkUser == null)
                {
                    // Lỗi 1: Số điện thoại không tồn tại
                    ViewBag.Error = "Số điện thoại này không tồn tại.";
                    ViewBag.ErrorType = "UserNotFound";
                    ViewBag.HasError = true;
                }
                else
                {
                    // Lỗi 2: Mật khẩu sai (SDT đúng nhưng Pass không khớp)
                    ViewBag.Error = "Mật khẩu không đúng.";
                    ViewBag.ErrorType = "WrongPassword";
                    ViewBag.HasError = true;
                }

                ViewBag.AttemptedUserName = SDT;
                return View();
            }
        }
        public ActionResult DangXuat()
        {
            // 1. Xóa tất cả các Session liên quan đến thông tin đăng nhập
            Session.Clear(); // Xóa tất cả Session trong phiên hiện tại
                             // HOẶC: Session.Abandon(); // Kết thúc toàn bộ Session

            // Nếu bạn chỉ muốn xóa các Session cụ thể:
            Session.Remove("UserID");
            Session.Remove("UserName");
            Session.Remove("SDT");
            Session.Remove("Avatar");
            // Session.Remove("ID_PhanQuyen"); // Nếu có

            // 2. Chuyển hướng người dùng về trang chủ (hoặc trang đăng nhập)
            return RedirectToAction("Index", "User"); // Chuyển về trang Index của Home Controller
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
