using PagedList; // <-- 1. THÊM THƯ VIỆN NÀY
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;



namespace QuanLyPhongTro.Controllers
{
    public class UserController : Controller
    {
        // Khởi tạo DbContext. Sử dụng DaTa_Phong_TroEntities1 dựa trên connection string bạn cung cấp
        private DaTa_Phong_TroEntities8 db = new DaTa_Phong_TroEntities8();
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

        public ActionResult Index(int? page,int? minPrice, int? maxPrice, int? minArea, int? maxArea, string filterBy = "de-xuat")
        {
            int? currentUserId = null;
            if (Session["UserID"] != null)
            {
                currentUserId = (int)Session["UserID"];
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
            ViewBag.CurrentFilter = filterBy; // <-- THÊM MỚI: Lưu filter hiện tại
            // ----- PHÂN TRANG -----
            int pageSize = 10; // Số item mỗi trang
            int pageNumber = (page ?? 1); // Trang hiện tại, nếu không có thì là trang 1

            // 3. SỬA CÂU TRUY VẤN
            var query = db.Phong_Tro
                          .Include(p => p.Tai_Khoan)
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Loai_Tin)
                          .Include(p => p.Video)
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
            IQueryable<Phong_Tro> sortedQuerys;
            switch (filterBy)
            {
                case "moi-dang": // Tab "Mới đăng"
                    // SỬA: Chỉ sắp xếp theo Ngày Đăng giảm dần (Mới nhất lên đầu)
                    // Không quan tâm ID_LoaiTin (VIP hay thường đều như nhau)
                    sortedQuery = query.OrderByDescending(p => p.Ngay_Dang);
                        break;

                case "co-video": // Nếu là tab "Có video"
                    sortedQuery = db.Phong_Tro
                     .Include(p => p.Tai_Khoan)
                     .Include(p => p.Khu_Vuc)
                     .Include(p => p.Hinh_Anh)
                     .Include(p => p.Loai_Tin)
                     .Include(p => p.Video)
                     .Where(p => p.Video.Any())
                     .OrderByDescending(p => p.Ngay_Dang);
                    break;

                case "de-xuat": // Nếu là tab "Đề xuất" (Mặc định)
                default:
                    sortedQuerys = query.OrderBy(p => p.ID_LoaiTin) // Ưu tiên Loại tin (VIP)
                                         .ThenByDescending(p => p.Ngay_Dang); // Sau đó mới nhất
                    break;
            }
            var paginatedPosts = sortedQuery.ToPagedList(pageNumber, pageSize);

            // 2. LẤY TOP 10 TIN MỚI NHẤT (CHO SIDEBAR)
            var newestPosts = db.Phong_Tro
                                .Include(p => p.Hinh_Anh) // Chỉ cần include ảnh
                                .OrderByDescending(p => p.Ngay_Dang)
                                .Take(10)
                                .ToList();

            // 3. TẠO VIEWMODEL VÀ GÁN DỮ LIỆU
            // 4. GỌI ToPagedList() THAY VÌ ToList()
            // Gửi Model phân trang qua View
            ViewBag.NewestPosts = newestPosts;
            ViewBag.PageTitle = "Kênh thông tin Phòng Trọ số 1 Việt Nam";
            return View(sortedQuery.ToPagedList(pageNumber, pageSize));
        }
        private List<int> GetUserFavorites()
        {
            if (Session["UserID"] != null)
            {
                int currentUserId = (int)Session["UserID"];
                return db.Yeu_Thich
                         .Where(yt => yt.ID_TK == currentUserId)
                         .Select(yt => yt.ID_Phong_Tro)
                         .ToList();
            }
            return new List<int>(); // Trả về danh sách rỗng nếu chưa đăng nhập
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleFavorite(int id)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                // Trả về JSON để JS xử lý, cho phép GET trong trường hợp này để không lỗi
                return Json(new { success = false, message = "Bạn cần đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            int currentUserId = (int)Session["UserID"];
            bool isFavorited = false;

            // 2. Kiểm tra xem tin này đã được yêu thích chưa
            var existingFavorite = db.Yeu_Thich
                                     .FirstOrDefault(yt => yt.ID_Phong_Tro == id && yt.ID_TK == currentUserId);

            if (existingFavorite != null)
            {
                // 3a. Bỏ thích
                db.Yeu_Thich.Remove(existingFavorite);
                isFavorited = false;
            }
            else
            {
                // 3b. Thêm thích
                var newFavorite = new Yeu_Thich
                {
                    ID_Phong_Tro = id,
                    ID_TK = currentUserId
                };
                db.Yeu_Thich.Add(newFavorite);
                isFavorited = true;
            }

            db.SaveChanges();

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
                          .Include(p => p.Video)
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
            ViewBag.PageTitle = "Cho Thuê Căn Hộ Chung Cư, Giá Rẻ, View Đẹp, Mới Nhất 2025";
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
                          .Include(p => p.Video)
                          .Where(p => p.ID_CD == 5); // <-- LỌC THEO ID_CD = 5

            var sortedQuery = query.OrderBy(p => p.ID_LoaiTin)
                                   .ThenByDescending(p => p.Ngay_Dang);

            // QUAN TRỌNG: Render ra View tên là "Index"
            ViewBag.PageTitle = "Cho Thuê Căn Hộ Mini + Chung Cư Mini Giá Rẻ, Mới Nhất 2025";
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
                             .Include(p => p.Video)
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
            if (phongTro.ID_KV.HasValue)
            {
                var relatedPosts = db.Phong_Tro
                    .Include(p => p.Hinh_Anh)
                    .Include(p => p.Khu_Vuc)
                    .Include(p => p.Loai_Tin) // Cần để hiển thị banner
                    .Include(p => p.Video)
                    .Where(p => p.ID_KV == phongTro.ID_KV.Value && p.ID_Phong_Tro != id) // Lọc cùng KV, trừ tin này
                    .OrderBy(p => p.ID_LoaiTin) // Sắp xếp theo ưu tiên
                    .ThenByDescending(p => p.Ngay_Dang)
                    .Take(8)
                    .ToList();

                ViewBag.RelatedPosts = relatedPosts;
            }

            // 2. Lấy Tin Đăng Mới Cập Nhật (Lấy 8 tin)
            var latestPosts = db.Phong_Tro
                .Include(p => p.Hinh_Anh)
                .Include(p => p.Khu_Vuc)
                .Include(p => p.Loai_Tin) // Cần để hiển thị banner
                .Include(p => p.Video)
                .Where(p => p.ID_Phong_Tro != id) // Trừ tin này
                .OrderByDescending(p => p.Ngay_Dang) // Mới nhất
                .Take(8)
                .ToList();

            ViewBag.LatestPosts = latestPosts;
            ViewBag.UserFavorites = userFavorites;

                var featuredPosts = db.Phong_Tro
            .Where(p => (p.ID_LoaiTin == 1 || p.ID_LoaiTin == 2) && p.ID_Phong_Tro != id)
            .OrderByDescending(p => p.Ngay_Dang)
            .Take(5)
            .ToList();

                ViewBag.FeaturedPosts = featuredPosts;
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
        public ActionResult TinDaLuu(int? page)
        {
            // --- Cấu hình phân trang (luôn cần) ---
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            // 1. Kiểm tra xem người dùng đã đăng nhập chưa
            if (Session["UserID"] == null)
            {
                // === SỬA LỖI NULL TẠI ĐÂY ===
                // Nếu chưa đăng nhập, tạo một danh sách rỗng
                // và gửi nó sang View để tránh lỗi Null.
                var emptyList = new List<Phong_Tro>().ToPagedList(pageNumber, pageSize);
                ViewBag.UserFavorites = new List<int>(); // Cũng gửi danh sách yêu thích rỗng

                // Trả về View với Model là danh sách rỗng
                return View(emptyList);
            }

            // --- Nếu đã đăng nhập, code cũ của bạn sẽ chạy ---
            int currentUserId = (int)Session["UserID"];

            // 2. Lấy danh sách ID các phòng trọ mà người này đã thích
            var favoritedIds = db.Yeu_Thich
                                 .Where(yt => yt.ID_TK == currentUserId)
                                 .Select(yt => yt.ID_Phong_Tro)
                                 .ToList();

            // 3. Lấy thông tin chi tiết của các phòng trọ đó
            var favoritedPostsQuery = db.Phong_Tro // Đổi tên biến để tránh nhầm lẫn
                                   .Include(p => p.Tai_Khoan)
                                   .Include(p => p.Khu_Vuc)
                                   .Include(p => p.Hinh_Anh)
                                   .Include(p => p.Loai_Tin)
                                   .Include(p => p.Video)
                                   .Where(p => favoritedIds.Contains(p.ID_Phong_Tro)) // Lọc theo danh sách ID đã lấy
                                   .OrderByDescending(p => p.Ngay_Dang); // Sắp xếp mới nhất

            // 4. Phân trang
            var pagedFavoritedPosts = favoritedPostsQuery.ToPagedList(pageNumber, pageSize);

            // 5. Gửi danh sách ID yêu thích sang View
            // (Việc này rất quan trọng để các nút trái tim hiển thị 'active' - màu đỏ)
            ViewBag.UserFavorites = favoritedIds;

            // 6. Gửi model đã phân trang sang View
            return View(pagedFavoritedPosts);
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
                Session["PhanQuyen"] = account.ID_Phan_Quyen;
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
            Session.Remove("PhanQuyen");
            // Session.Remove("ID_PhanQuyen"); // Nếu có

            // 2. Chuyển hướng người dùng về trang chủ (hoặc trang đăng nhập)
            return RedirectToAction("Index", "User"); // Chuyển về trang Index của Home Controller
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
        public ActionResult TimKiem(List<int> kvIds, int? page)
        {
            var userFavorites = GetUserFavorites();
            ViewBag.UserFavorites = userFavorites;

            int pageSize = 10;
            int pageNumber = page ?? 1;

            var query = db.Phong_Tro
                          .Include(p => p.Tai_Khoan)
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Loai_Tin)
                          .Include(p => p.Video);

            // Nếu có danh sách khu vực → lọc
            if (kvIds != null && kvIds.Any())
            {
                query = query.Where(p => p.ID_KV.HasValue && kvIds.Contains(p.ID_KV.Value));
            }
            // Lấy tên khu vực đã chọn để hiển thị
            List<string> khuVucNames = new List<string>();

            if (kvIds != null && kvIds.Any())
            {
                khuVucNames = db.Khu_Vuc
                                .Where(kv => kvIds.Contains(kv.ID_KV))
                                .Select(kv => kv.Ten_KV)
                                .ToList();
            }

            // Gán tiêu đề trang
            if (khuVucNames.Any())
            {
                ViewBag.PageTitle = "Kết quả tìm kiếm tại : " + string.Join(", ", khuVucNames);
            }
            else
            {
                ViewBag.PageTitle = "Kết quả tìm kiếm";
            }
            var sortedQuery = query.OrderBy(p => p.ID_LoaiTin)
                                   .ThenByDescending(p => p.Ngay_Dang);

            var newestPosts = db.Phong_Tro
                                .Include(p => p.Hinh_Anh)
                                .OrderByDescending(p => p.Ngay_Dang)
                                .Take(10)
                                .ToList();

            ViewBag.NewestPosts = newestPosts;

            // Render lại View Index
            return View("Index", sortedQuery.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult BoLoc(
            int? ID_CD,
            int? ID_KV,
            int? minPrice,
            int? maxPrice,
            int? minArea,
            int? maxArea,
            int? page
        )
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var query = db.Phong_Tro
                          .Include(p => p.Tai_Khoan)
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Loai_Tin)
                          .Include(p => p.Video)
                          .AsQueryable();

            // Danh mục
            if (ID_CD.HasValue)
                query = query.Where(p => p.ID_CD == ID_CD.Value);

            // Khu vực
            if (ID_KV.HasValue)
                query = query.Where(p => p.ID_KV == ID_KV.Value);

            // Giá
            if (minPrice.HasValue)
                query = query.Where(p => p.Gia_Ca >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Gia_Ca < maxPrice.Value);

            // Diện tích
            if (minArea.HasValue)
                query = query.Where(p => p.Dien_Tich >= minArea.Value);
            if (maxArea.HasValue)
                query = query.Where(p => p.Dien_Tich <= maxArea.Value);

            // Sắp xếp
            query = query.OrderBy(p => p.ID_LoaiTin)
                         .ThenByDescending(p => p.Ngay_Dang);

            var result = query.ToPagedList(pageNumber, pageSize);

            // Set title theo danh mục
            if (ID_CD == 4) ViewBag.PageTitle = "Cho thuê Phòng trọ";
            else if (ID_CD == 5) ViewBag.PageTitle = "Cho thuê Nhà ở Mini";
            else if (ID_CD == 6) ViewBag.PageTitle = "Cho thuê Căn hộ Chung cư";
            else ViewBag.PageTitle = "Kết quả lọc";

            // Gán ViewBag để phục hồi state UI
            ViewBag.SelectedKV = ID_KV;           // int? hoặc null

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinArea = minArea;
            ViewBag.MaxArea = maxArea;

            if (ID_CD == 4) ViewBag.SelectedCategory = "phongtro";
            else if (ID_CD == 6) ViewBag.SelectedCategory = "canhochungcu";
            else if (ID_CD == 5) ViewBag.SelectedCategory = "canhomini";
            else ViewBag.SelectedCategory = "";


            // Lấy newest posts nếu cần (như Index)
            var newestPosts = db.Phong_Tro
                                .Include(p => p.Hinh_Anh)
                                .OrderByDescending(p => p.Ngay_Dang)
                                .Take(10)
                                .ToList();
            ViewBag.NewestPosts = newestPosts;

            return View("Index", result);
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
