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
