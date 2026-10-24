using QuanLyPhongTro.Areas.Admin.ChuNhaViewModels;
using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace QuanLyPhongTro.Areas.Host.Controllers
    {
    public class AdminController : BaseController
        {

        public ActionResult Index()
            {
            db.Configuration.ProxyCreationEnabled = false; // tránh vòng tham chiếu
            var vm = new ChuNha
                {
                KhuVuc = db.Khu_Vuc.ToList(),
                LoaiTin = db.Loai_Tin.ToList(),
                BangGiaTin = db.Bang_Gia_Tin
                    .Select(x => new BangGiaTinDTO
                        {
                        ID_Gia = x.ID_Gia,
                        ID_LoaiTin = x.ID_LoaiTin ?? 0,
                        Thoi_Gian = x.Thoi_Gian,
                        Gia_Goc = x.Gia_Goc ?? 0,
                        Gia_Giam = x.Gia_Giam ?? 0,
                        Ghi_Chu = x.Ghi_Chu
                        })
                    .ToList()
                };
            return View(vm);
            }


        [HttpPost]
        public ActionResult SavePost(
            string TieuDe,
            string MoTa,
            double GiaThue,
            float DienTich,
            string DiaChi,
            int ID_KV,
            int ID_CD,
            int ID_LoaiTin,
            double BangGia,
            int Ngay,
            HttpPostedFileBase[] imageInput,
            HttpPostedFileBase[] videoInput

        )
            {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);
            string chuanhoa_mota = "";
            if (!string.IsNullOrEmpty(MoTa))
                {
                chuanhoa_mota = MoTa
                    .Replace("\r\n", "##split##")
                    .Replace("\n", "##split##")
                    .Replace("\r", "##split##");
                }

            //  Tạo phòng trọ mới
            var pt = new Phong_Tro
                {
                ID_KV = ID_KV,
                ID_CD = ID_CD,
                ID_LoaiTin = ID_LoaiTin,
                ID_TK = maTK,
                Ten_Phong = TieuDe,
                Dien_Tich = DienTich,
                Dia_Chi = DiaChi,
                Mo_Ta = chuanhoa_mota,
                Gia_Ca = (decimal?)GiaThue,
                Ngay_Dang = DateTime.Now,
                Ngay_Het_Han = DateTime.Now.AddDays(Ngay),
                Gia_Duyet = (decimal)BangGia,
                Trang_Thai = false
                };

            db.Phong_Tro.Add(pt);
            db.SaveChanges(); // 🔹 Lưu để sinh ID_Phong_Tro tự động
            //  Lưu đặc điểm nổi bật
            var noiBat = new Noi_Bat
                {
                ID_Phong_Tro = pt.ID_Phong_Tro,
                Day_du_noi_that = Request["DayDuNoiThat"] != null,
                Co_may_lanh = Request["CoMayLanh"] != null,
                Co_thang_may = Request["CoThangMay"] != null,
                Co_bao_ve_24_24 = Request["CoBaoVe"] != null,
                Co_gac = Request["CoGac"] != null,
                Co_may_giat = Request["CoMayGiat"] != null,
                Khong_chung_chu = Request["KhongChungChu"] != null,
                Ke_bep = Request["CoKeBep"] != null,
                Co_tu_lanh = Request["CoTuLanh"] != null,
                Gio_giac_tu_do = Request["GioGiacTuDo"] != null
                };
            db.Noi_Bat.Add(noiBat);


            /*  LƯU NHIỀU ẢNH */
            if (imageInput != null && imageInput.Length > 0)
                {
                string imgFolder = Server.MapPath("~/Kho/Img/");
                Directory.CreateDirectory(imgFolder);

                var rnd = new Random();
                foreach (var file in imageInput)
                    {
                    if (file == null || file.ContentLength == 0) continue;

                    try
                        {
                        string ext = Path.GetExtension(file.FileName);
                        string imgName = $"{pt.ID_Phong_Tro}_{rnd.Next(1000, 9999)}{ext}";
                        string imgPath = Path.Combine(imgFolder, imgName);

                        System.Diagnostics.Debug.WriteLine("👉 Save to: " + imgPath);
                        file.SaveAs(imgPath);

                        db.Hinh_Anh.Add(new Hinh_Anh
                            {
                            ID_Phong_Tro = pt.ID_Phong_Tro,
                            Url_Anh = imgName
                            });
                        }
                    catch (Exception ex)
                        {
                        System.Diagnostics.Debug.WriteLine("Lỗi lưu ảnh: " + ex.ToString());
                        }
                    }
                }
            /*  LƯU NHIỀU VIDEO */
            if (videoInput != null && videoInput.Length > 0)
                {
                string videoFolder = Server.MapPath("~/Kho/Video/");
                Directory.CreateDirectory(videoFolder);

                var rnd = new Random();

                foreach (var file in videoInput)
                    {
                    if (file == null || file.ContentLength == 0) continue;

                    try
                        {
                        string ext = Path.GetExtension(file.FileName);
                        string videoName = $"{pt.ID_Phong_Tro}_{rnd.Next(1000, 9999)}{ext}";
                        string videoPath = Path.Combine(videoFolder, videoName);
                        System.Diagnostics.Debug.WriteLine("👉 Save video to: " + videoPath);
                        file.SaveAs(videoPath);
                        db.Video.Add(new Video
                            {
                            ID_Phong_Tro = pt.ID_Phong_Tro,
                            Url_Video = videoName
                            });
                        }
                    catch (Exception ex)
                        {
                        System.Diagnostics.Debug.WriteLine("Lỗi lưu video: " + ex.ToString());
                        }
                    }
                }
            try
                {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Vui lòng liên hệ quản trị viên để duyệt";
                return RedirectToAction("DSTinDang");
                }
            catch (Exception ex)
                {
                System.Diagnostics.Debug.WriteLine("Lỗi khi lưu bài đăng (db.SaveChanges): " + ex.ToString());
                TempData["Error"] = "Có lỗi xảy ra khi lưu bài đăng: " + (ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("DSTinDang");
                }
            }
        public ActionResult DSTinDang()
            {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);
            var listPhong = db.Phong_Tro
                .Where(x => x.ID_TK == maTK)
                .Select(x => new PhongTroListVM
                    {
                    ID_Phong_Tro = x.ID_Phong_Tro,
                    Ten_Phong = x.Ten_Phong,
                    Ngay_Dang = x.Ngay_Dang,
                    Trang_Thai = x.Trang_Thai
                    })
                .ToList();

            var vm = new ChuNha
                {
                PhongTros = listPhong
                // nếu view này cần thêm KhuVuc, LoaiTin thì gán tiếp
                };

            return View(vm);
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePhongTro(int id)
            {
            // tìm phòng
            var phong = db.Phong_Tro.FirstOrDefault(x => x.ID_Phong_Tro == id);
            if (phong == null)
                {
                return RedirectToAction("DSTinDang");
                }

            // xóa các bảng liên quan
            // 1. hình ảnh
            var imgs = db.Hinh_Anh.Where(x => x.ID_Phong_Tro == id).ToList();
            foreach (var img in imgs)
                {
                var path = Server.MapPath("~/Kho/Img/" + img.Url_Anh);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                }
            db.Hinh_Anh.RemoveRange(imgs);
            // 2. video
            var videos = db.Video.Where(x => x.ID_Phong_Tro == id).ToList();
            foreach (var v in videos)
                {
                var path = Server.MapPath("~/Kho/Video/" + v.Url_Video);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                }
            db.Video.RemoveRange(videos);
            var noiBat = db.Noi_Bat.Where(x => x.ID_Phong_Tro == id).ToList();
            db.Noi_Bat.RemoveRange(noiBat);

            // 4. xóa phòng trọ
            db.Phong_Tro.Remove(phong);

            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa thành công";
            return RedirectToAction("DSTinDang");
            }

       

        public ActionResult UpdateData(int id)
            {
            // lấy phòng trọ
            var phong = db.Phong_Tro.FirstOrDefault(x => x.ID_Phong_Tro == id);
            if (phong == null)
                {
                return HttpNotFound();
                }

            // lấy đặc điểm nổi bật (có thể null)
            var noiBat = db.Noi_Bat.FirstOrDefault(x => x.ID_Phong_Tro == id);

            // lấy user để đổ vào viewbag như cũ
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var user = db.Tai_Khoan
                .Where(x => x.ID_Tk == maTK)
                .Select(x => new { x.Name, x.SDT, x.Avata })
                .FirstOrDefault();

            ViewBag.Name = user?.Name ?? "";
            ViewBag.SDT = user?.SDT ?? "";
            ViewBag.Avata = user?.Avata ?? "";

            // tạo viewmodel
            var vm = new ChuNha
                {
                HinhAnhs = db.Hinh_Anh.Where(x => x.ID_Phong_Tro == id).Select(x => x.Url_Anh).ToList(),
                Videos = db.Video.Where(x => x.ID_Phong_Tro == id).Select(x => x.Url_Video).ToList(),
                // list để đổ dropdown
                KhuVuc = db.Khu_Vuc.ToList(),
                LoaiTin = db.Loai_Tin.ToList(),
                BangGiaTin = db.Bang_Gia_Tin.Select(x => new BangGiaTinDTO
                    {
                    ID_Gia = x.ID_Gia,
                    ID_LoaiTin = x.ID_LoaiTin ?? 0,
                    Thoi_Gian = x.Thoi_Gian,
                    Gia_Goc = x.Gia_Goc ?? 0,
                    Gia_Giam = x.Gia_Giam ?? 0,
                    Ghi_Chu = x.Ghi_Chu
                    }).ToList(),

                // dữ liệu phòng để hiển thị lại
                ID_Phong_Tro = phong.ID_Phong_Tro,
                ID_KV = phong.ID_KV,
                ID_CD = phong.ID_CD,
                ID_LoaiTin = phong.ID_LoaiTin,
                TieuDe = phong.Ten_Phong,
                MoTa = phong.Mo_Ta,
                GiaThue = (double?)phong.Gia_Ca,
                DienTich = (float)phong.Dien_Tich,
                DiaChi = phong.Dia_Chi,
                Ngay = (phong.Ngay_Het_Han - phong.Ngay_Dang)?.Days, // nếu bạn muốn tính lại số ngày
                BangGia = phong.Gia_Duyet,

                // đặc điểm
                DayDuNoiThat = noiBat?.Day_du_noi_that ?? false,
                CoMayLanh = noiBat?.Co_may_lanh ?? false,
                CoThangMay = noiBat?.Co_thang_may ?? false,
                CoBaoVe = noiBat?.Co_bao_ve_24_24 ?? false,
                CoGac = noiBat?.Co_gac ?? false,
                CoMayGiat = noiBat?.Co_may_giat ?? false,
                KhongChungChu = noiBat?.Khong_chung_chu ?? false,
                CoKeBep = noiBat?.Ke_bep ?? false,
                CoTuLanh = noiBat?.Co_tu_lanh ?? false,
                GioGiacTuDo = noiBat?.Gio_giac_tu_do ?? false
                };

            return View(vm);
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
          public ActionResult Updates(
          int ID_Phong_Tro,
          string TieuDe,
          string MoTa,
          decimal GiaThue,
          float DienTich,
          string DiaChi,
          int ID_KV,
          int ID_CD,
          int ID_LoaiTin,
          decimal BangGia,
          int Ngay,
          HttpPostedFileBase[] imageInput,
          HttpPostedFileBase[] videoInput
      )
            {
            // 1. Lấy phòng
            var phong = db.Phong_Tro.FirstOrDefault(x => x.ID_Phong_Tro == ID_Phong_Tro);
            if (phong == null)
                {
                TempData["Error"] = "Không tìm thấy phòng trọ cần cập nhật.";
                return RedirectToAction("DSTinDang");
                }

            // 2. Cập nhật thông tin chính
            phong.Ten_Phong = TieuDe;
            phong.Mo_Ta = MoTa;
            phong.Gia_Ca = GiaThue;
            phong.Dien_Tich = DienTich;
            phong.Dia_Chi = DiaChi;
            phong.ID_KV = ID_KV;
            phong.ID_CD = ID_CD;
            phong.ID_LoaiTin = ID_LoaiTin;
            phong.Gia_Duyet = BangGia;
            phong.Ngay_Het_Han = DateTime.Now.AddDays(Ngay);
            phong.Trang_Thai = false; // cập nhật xong chờ duyệt lại

            db.SaveChanges();

            // 3. Cập nhật đặc điểm nổi bật
            var noiBat = db.Noi_Bat.FirstOrDefault(x => x.ID_Phong_Tro == ID_Phong_Tro);
            if (noiBat == null)
                {
                noiBat = new Noi_Bat { ID_Phong_Tro = ID_Phong_Tro };
                db.Noi_Bat.Add(noiBat);
                }
            noiBat.Day_du_noi_that = Request["DayDuNoiThat"] != null;
            noiBat.Co_may_lanh = Request["CoMayLanh"] != null;
            noiBat.Co_thang_may = Request["CoThangMay"] != null;
            noiBat.Co_bao_ve_24_24 = Request["CoBaoVe"] != null;
            noiBat.Co_gac = Request["CoGac"] != null;
            noiBat.Co_may_giat = Request["CoMayGiat"] != null;
            noiBat.Khong_chung_chu = Request["KhongChungChu"] != null;
            noiBat.Ke_bep = Request["CoKeBep"] != null;
            noiBat.Co_tu_lanh = Request["CoTuLanh"] != null;
            noiBat.Gio_giac_tu_do = Request["GioGiacTuDo"] != null;

            db.SaveChanges();

            // 4. ẢNH CŨ: giữ lại cái user không xóa (nếu view có gửi OldImages lên)
            // nếu bạn chưa làm phần này ở view thì có thể bỏ hẳn block này đi
            var oldImages = Request.Form.GetValues("OldImages")?.ToList() ?? new List<string>();
            var existedImgs = db.Hinh_Anh.Where(x => x.ID_Phong_Tro == ID_Phong_Tro).ToList();
            foreach (var img in existedImgs)
                {
                if (!oldImages.Contains(img.Url_Anh))
                    {
                    // xóa file vật lý
                    var path = Server.MapPath("~/Kho/Img/" + img.Url_Anh);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);

                    db.Hinh_Anh.Remove(img);
                    }
                }
            db.SaveChanges();

            // 5. Thêm ảnh mới (nếu người dùng upload thêm)
            if (imageInput != null && imageInput.Length > 0)
                {
                string imgFolder = Server.MapPath("~/Kho/Img/");
                Directory.CreateDirectory(imgFolder);
                var rnd = new Random();

                foreach (var file in imageInput)
                    {
                    if (file == null || file.ContentLength == 0) continue;

                    string ext = Path.GetExtension(file.FileName);
                    string imgName = $"{ID_Phong_Tro}_{rnd.Next(1000, 9999)}{ext}";
                    string imgPath = Path.Combine(imgFolder, imgName);

                    file.SaveAs(imgPath);

                    db.Hinh_Anh.Add(new Hinh_Anh
                        {
                        ID_Phong_Tro = ID_Phong_Tro,
                        Url_Anh = imgName
                        });
                    }
                }

            // 6. VIDEO CŨ nếu bạn cũng muốn xử lý thì làm giống ảnh ở đây

            // 7. Thêm video mới
            if (videoInput != null && videoInput.Length > 0)
                {
                string videoFolder = Server.MapPath("~/Kho/Video/");
                Directory.CreateDirectory(videoFolder);
                var rnd = new Random();

                foreach (var file in videoInput)
                    {
                    if (file == null || file.ContentLength == 0) continue;

                    string ext = Path.GetExtension(file.FileName);
                    string videoName = $"{ID_Phong_Tro}_{rnd.Next(1000, 9999)}{ext}";
                    string videoPath = Path.Combine(videoFolder, videoName);

                    file.SaveAs(videoPath);

                    db.Video.Add(new Video
                        {
                        ID_Phong_Tro = ID_Phong_Tro,
                        Url_Video = videoName
                        });
                    }
                }

            db.SaveChanges();
            TempData["Success"] = "Cập nhật tin thành công!";
            return RedirectToAction("DSTinDang");
            }

        public ActionResult QuanlyTaiKhoan()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatThongTin(string SDT, string Name)
            {
            // kiểm tra đăng nhập
            if (Session["UserID"] == null)
                {
                TempData["Error"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("DangNhap", "TaiKhoan");
                }

            int maTK = Convert.ToInt32(Session["UserID"]);
            var tk = db.Tai_Khoan.FirstOrDefault(x => x.ID_Tk == maTK);
            if (tk == null)
                {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("QuanlyTaiKhoan");
                }

            // cập nhật
            tk.SDT = SDT;
            tk.Name = Name;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật thông tin thành công!";
            TempData["ActiveTab"] = "thongtin";   // để quay lại đúng tab
            return RedirectToAction("QuanlyTaiKhoan");
            }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatAnhDaiDien(HttpPostedFileBase AvtFile)
            {
            // 🔒 Kiểm tra đăng nhập
            if (Session["UserID"] == null)
                {
                TempData["Error"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("DangNhap", "TaiKhoan");
                }

            int maTK = Convert.ToInt32(Session["UserID"]);
            var tk = db.Tai_Khoan.FirstOrDefault(x => x.ID_Tk == maTK);

            if (tk == null)
                {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("QuanlyTaiKhoan");
                }

            if (AvtFile != null && AvtFile.ContentLength > 0)
                {
                string folder = Server.MapPath("~/Assets/Images/Avatar/");
                Directory.CreateDirectory(folder);

                string ext = Path.GetExtension(AvtFile.FileName);
                string fileName = $"avt_{maTK}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                string path = Path.Combine(folder, fileName);

                // Lưu ảnh mới
                AvtFile.SaveAs(path);

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(tk.Avata))
                    {
                    string oldPath = Path.Combine(folder, tk.Avata);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                    }

                // Cập nhật DB
                tk.Avata = fileName;
                db.SaveChanges();

                TempData["Success"] = "Ảnh đại diện đã được cập nhật!";
                }
            else
                {
                TempData["Error"] = "Vui lòng chọn ảnh hợp lệ.";
                }

            TempData["ActiveTab"] = "thongtin";
            return RedirectToAction("QuanlyTaiKhoan");
            }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(string OldPassword, string NewPassword, string ConfirmPassword)
            {
            // lấy lại mấy thứ để view còn hiển thị
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var tk = db.Tai_Khoan.FirstOrDefault(x => x.ID_Tk == maTK);
            if (tk == null)
                {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                ViewBag.ActiveTab = "doimatkhau";
                return View("QuanlyTaiKhoan");
                }

            // 1. mật khẩu cũ sai
            if (tk.Pass != OldPassword)
                {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng.");
                ViewBag.ActiveTab = "doimatkhau";
                return View("QuanlyTaiKhoan");
                }

            // 2. mật khẩu mới trống
            if (string.IsNullOrWhiteSpace(NewPassword))
                {
                ModelState.AddModelError("NewPassword", "Vui lòng nhập mật khẩu mới.");
                ViewBag.ActiveTab = "doimatkhau";
                return View("QuanlyTaiKhoan");
                }

            // 3. xác nhận không khớp
            if (NewPassword != ConfirmPassword)
                {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                ViewBag.ActiveTab = "doimatkhau";
                return View("QuanlyTaiKhoan");
                }

            // 4. ok -> lưu
            tk.Pass = NewPassword;
            db.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            TempData["ActiveTab"] = "doimatkhau";
            return RedirectToAction("QuanlyTaiKhoan");

            }

        [HttpGet]
        public ActionResult DuDoanLapDay(int? idKv, int? forecastMonths)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            int months = forecastMonths ?? 6;
            string connStr = db.Database.Connection.ConnectionString;

            var myRooms = new List<HostRoomStatusItem>();
            int tongPhong = 0;
            int phongDaThue = 0;
            int? primaryKvId = idKv;

            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                {
                    conn.Open();
                    using (var checkCmd = new System.Data.SqlClient.SqlCommand(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Phong_Tro') AND name = 'Da_Thue')
                        BEGIN
                            ALTER TABLE Phong_Tro ADD Da_Thue BIT NOT NULL CONSTRAINT DF_Phong_Tro_Da_Thue DEFAULT 0;
                            UPDATE p SET p.Da_Thue = 1 FROM Phong_Tro p WHERE EXISTS (SELECT 1 FROM Hop_Dong hd WHERE hd.ID_Phong_Tro = p.ID_Phong_Tro AND hd.Trang_Thai = 1);
                        END
                    ", conn))
                    {
                        checkCmd.ExecuteNonQuery();
                    }

                    string sql = @"
                        SELECT p.ID_Phong_Tro, p.Ten_Phong, p.Gia_Ca, ISNULL(k.Ten_KV, N'Chưa phân loại') as Ten_KV, 
                               ISNULL(p.Da_Thue, 0) as Da_Thue, p.Ngay_Dang, p.ID_KV
                        FROM Phong_Tro p
                        LEFT JOIN Khu_Vuc k ON p.ID_KV = k.ID_KV
                        WHERE p.ID_TK = @IdTk
                        ORDER BY p.ID_Phong_Tro DESC
                    ";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdTk", maTK);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bool daThue = Convert.ToBoolean(reader["Da_Thue"]);
                                int roomKv = reader["ID_KV"] != DBNull.Value ? Convert.ToInt32(reader["ID_KV"]) : 0;
                                if (!primaryKvId.HasValue && roomKv > 0)
                                {
                                    primaryKvId = roomKv;
                                }

                                myRooms.Add(new HostRoomStatusItem
                                {
                                    IdPhong = Convert.ToInt32(reader["ID_Phong_Tro"]),
                                    TenPhong = reader["Ten_Phong"].ToString(),
                                    GiaThue = reader["Gia_Ca"] != DBNull.Value ? Convert.ToDecimal(reader["Gia_Ca"]) : 0,
                                    TenKhuVuc = reader["Ten_KV"].ToString(),
                                    DaThue = daThue,
                                    NgayDang = reader["Ngay_Dang"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["Ngay_Dang"]) : null
                                });

                                tongPhong++;
                                if (daThue) phongDaThue++;
                            }
                        }
                    }
                }
            }
            catch
            {
                var fallbackRooms = db.Phong_Tro.Where(p => p.ID_TK == maTK).OrderByDescending(p => p.ID_Phong_Tro).ToList();
                foreach (var p in fallbackRooms)
                {
                    bool daThue = p.Hop_Dong.Any(h => h.Trang_Thai);
                    int roomKv = p.ID_KV ?? 0;
                    if (!primaryKvId.HasValue && roomKv > 0)
                    {
                        primaryKvId = roomKv;
                    }

                    myRooms.Add(new HostRoomStatusItem
                    {
                        IdPhong = p.ID_Phong_Tro,
                        TenPhong = p.Ten_Phong,
                        GiaThue = p.Gia_Ca ?? 0,
                        TenKhuVuc = p.Khu_Vuc != null ? p.Khu_Vuc.Ten_KV : "Chưa phân loại",
                        DaThue = daThue,
                        NgayDang = p.Ngay_Dang
                    });

                    tongPhong++;
                    if (daThue) phongDaThue++;
                }
            }

            double tyLeLapDayCuaToi = tongPhong > 0 ? Math.Round(((double)phongDaThue / tongPhong) * 100, 1) : 0;

            var service = new OccupancyForecastService(connStr);
            var forecast = service.CalculateForecast(primaryKvId, months);

            var vm = new ChuNhaForecastVM
            {
                Forecast = forecast,
                TongPhongCuaToi = tongPhong,
                PhongDaThueCuaToi = phongDaThue,
                TyLeLapDayCuaToi = tyLeLapDayCuaToi,
                SelectedMonths = months,
                SelectedIdKv = primaryKvId,
                TenKhuVucHienTai = forecast.DistrictName,
                DanhSachPhongCuaToi = myRooms,
                KhuVuc = db.Khu_Vuc.OrderBy(k => k.Ten_KV).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult ToggleTrangThaiThue(int idPhong)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            string connStr = db.Database.Connection.ConnectionString;
            bool newState = false;

            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                {
                    conn.Open();
                    using (var checkCmd = new System.Data.SqlClient.SqlCommand(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Phong_Tro') AND name = 'Da_Thue')
                        BEGIN
                            ALTER TABLE Phong_Tro ADD Da_Thue BIT NOT NULL CONSTRAINT DF_Phong_Tro_Da_Thue DEFAULT 0;
                            UPDATE p SET p.Da_Thue = 1 FROM Phong_Tro p WHERE EXISTS (SELECT 1 FROM Hop_Dong hd WHERE hd.ID_Phong_Tro = p.ID_Phong_Tro AND hd.Trang_Thai = 1);
                        END
                    ", conn))
                    {
                        checkCmd.ExecuteNonQuery();
                    }

                    string query = @"
                        UPDATE Phong_Tro 
                        SET Da_Thue = CASE WHEN ISNULL(Da_Thue, 0) = 1 THEN 0 ELSE 1 END
                        OUTPUT INSERTED.Da_Thue
                        WHERE ID_Phong_Tro = @IdPhong AND ID_TK = @IdTk
                    ";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdPhong", idPhong);
                        cmd.Parameters.AddWithValue("@IdTk", maTK);
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            newState = Convert.ToBoolean(res);
                            return Json(new { success = true, newState = newState });
                        }
                    }
                }
            }
            catch
            {
            }

            return Json(new { success = false, message = "Không tìm thấy phòng hoặc không có quyền thao tác." });
        }

        public ActionResult ThongKeDoanhThu(int? thang, int? nam)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var query = db.Hoa_Don_Dien_Nuoc.Where(h => h.Hop_Dong.Phong_Tro.ID_TK == maTK);

            var danhSachNam = query.Select(h => h.Nam).Distinct().OrderByDescending(x => x).ToList();
            if (!danhSachNam.Any())
            {
                danhSachNam.Add(DateTime.Now.Year);
            }

            var filteredQuery = query;
            if (nam.HasValue && nam.Value > 0)
            {
                filteredQuery = filteredQuery.Where(h => h.Nam == nam.Value);
            }
            if (thang.HasValue && thang.Value > 0)
            {
                filteredQuery = filteredQuery.Where(h => h.Thang == thang.Value);
            }

            var listHoaDon = filteredQuery
                .OrderByDescending(h => h.Nam)
                .ThenByDescending(h => h.Thang)
                .ThenByDescending(h => h.ID_Hoa_Don)
                .Select(h => new HoaDonChiTietItem
                {
                    ID_Hoa_Don = h.ID_Hoa_Don,
                    TenPhong = h.Hop_Dong.Phong_Tro.Ten_Phong,
                    TenKhach = h.Hop_Dong.Khach_Thue.Ho_Ten,
                    SDT = h.Hop_Dong.Khach_Thue.SDT,
                    Thang = h.Thang,
                    Nam = h.Nam,
                    SoDien = h.So_Dien,
                    TienDien = h.Tien_Dien,
                    SoNuoc = h.So_Nuoc,
                    TienNuoc = h.Tien_Nuoc,
                    TienPhong = h.Tien_Phong,
                    TienWifi = h.Tien_Wifi,
                    TongTien = h.Tong_Tien,
                    NgayLap = h.Ngay_Lap,
                    HanThanhToan = h.Han_Thanh_Toan,
                    TrangThai = h.Trang_Thai
                })
                .ToList();

            var groupTheoThang = query
                .GroupBy(h => new { h.Nam, h.Thang })
                .Select(g => new DoanhThuThangItem
                {
                    Nam = g.Key.Nam,
                    Thang = g.Key.Thang,
                    SoHoaDon = g.Count(),
                    TienPhong = g.Sum(x => (decimal?)x.Tien_Phong) ?? 0,
                    TienDien = g.Sum(x => (decimal?)x.Tien_Dien) ?? 0,
                    TienNuoc = g.Sum(x => (decimal?)x.Tien_Nuoc) ?? 0,
                    TienWifi = g.Sum(x => (decimal?)x.Tien_Wifi) ?? 0,
                    TongCong = g.Sum(x => (decimal?)x.Tong_Tien) ?? 0,
                    DaThu = g.Where(x => x.Trang_Thai).Sum(x => (decimal?)x.Tong_Tien) ?? 0,
                    ChuaThu = g.Where(x => !x.Trang_Thai).Sum(x => (decimal?)x.Tong_Tien) ?? 0
                })
                .OrderByDescending(g => g.Nam)
                .ThenByDescending(g => g.Thang)
                .ToList();

            var vm = new ThongKeDoanhThuVM
            {
                TongDoanhThu = listHoaDon.Sum(x => x.TongTien),
                DaThu = listHoaDon.Where(x => x.TrangThai).Sum(x => x.TongTien),
                ChuaThu = listHoaDon.Where(x => !x.TrangThai).Sum(x => x.TongTien),
                TongTienPhong = listHoaDon.Sum(x => x.TienPhong),
                TongTienDien = listHoaDon.Sum(x => x.TienDien),
                TongTienNuoc = listHoaDon.Sum(x => x.TienNuoc),
                TongTienWifi = listHoaDon.Sum(x => x.TienWifi),
                SelectedThang = thang,
                SelectedNam = nam,
                DanhSachNam = danhSachNam,
                DoanhThuTheoThang = groupTheoThang,
                DanhSachHoaDon = listHoaDon
            };

            return View(vm);
        }

        public ActionResult QuanLyPhongTro(string search, int? idKv, string status)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var query = db.Phong_Tro.Where(p => p.ID_TK == maTK);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Ten_Phong.Contains(search) || p.Dia_Chi.Contains(search));
            }
            if (idKv.HasValue && idKv.Value > 0)
            {
                query = query.Where(p => p.ID_KV == idKv.Value);
            }

            var listPhongRaw = query.OrderByDescending(p => p.ID_Phong_Tro).ToList();

            var listPhongVM = listPhongRaw.Select(p =>
            {
                var hdHienTai = p.Hop_Dong
                    .Where(h => h.Trang_Thai)
                    .OrderByDescending(h => h.ID_Hop_Dong)
                    .FirstOrDefault();

                return new PhongTroQuanLyItem
                {
                    ID_Phong_Tro = p.ID_Phong_Tro,
                    TenPhong = p.Ten_Phong,
                    TenKhuVuc = p.Khu_Vuc != null ? p.Khu_Vuc.Ten_KV : "",
                    DiaChi = p.Dia_Chi,
                    DienTich = p.Dien_Tich,
                    GiaThue = p.Gia_Ca,
                    NgayDang = p.Ngay_Dang,
                    NgayHetHan = p.Ngay_Het_Han,
                    TrangThaiDuyet = p.Trang_Thai ?? false,
                    DangCoKhachThue = hdHienTai != null,
                    TenKhachThue = hdHienTai != null && hdHienTai.Khach_Thue != null ? hdHienTai.Khach_Thue.Ho_Ten : "",
                    SDTKhachThue = hdHienTai != null && hdHienTai.Khach_Thue != null ? hdHienTai.Khach_Thue.SDT : "",
                    NgayBatDauThue = hdHienTai != null ? (DateTime?)hdHienTai.Ngay_Bat_Dau : null,
                    NgayKetThucThue = hdHienTai != null ? hdHienTai.Ngay_Ket_Thuc : null
                };
            }).ToList();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "dangthue")
                    listPhongVM = listPhongVM.Where(p => p.DangCoKhachThue).ToList();
                else if (status == "trong")
                    listPhongVM = listPhongVM.Where(p => !p.DangCoKhachThue).ToList();
            }

            var lichSuThue = db.Hop_Dong
                .Where(h => h.Phong_Tro.ID_TK == maTK)
                .OrderByDescending(h => h.ID_Hop_Dong)
                .Select(h => new LichSuThuePhongItem
                {
                    ID_Hop_Dong = h.ID_Hop_Dong,
                    ID_Phong_Tro = h.ID_Phong_Tro,
                    TenPhong = h.Phong_Tro.Ten_Phong,
                    TenKhach = h.Khach_Thue != null ? h.Khach_Thue.Ho_Ten : "",
                    SDT = h.Khach_Thue != null ? h.Khach_Thue.SDT : "",
                    CCCD = h.Khach_Thue != null ? h.Khach_Thue.CCCD : "",
                    NgayBatDau = h.Ngay_Bat_Dau,
                    NgayKetThuc = h.Ngay_Ket_Thuc,
                    GiaThue = h.Gia_Thue,
                    TrangThai = h.Trang_Thai
                })
                .ToList();

            var vm = new QuanLyPhongTroVM
            {
                TongSoPhong = listPhongRaw.Count,
                SoPhongDangThue = listPhongRaw.Count(p => p.Hop_Dong.Any(h => h.Trang_Thai)),
                SoPhongTrong = listPhongRaw.Count(p => !p.Hop_Dong.Any(h => h.Trang_Thai)),
                TongGiaTriThueThang = listPhongRaw.Sum(p => p.Gia_Ca ?? 0),
                DanhSachPhong = listPhongVM,
                LichSuThue = lichSuThue
            };

            ViewBag.KhuVuc = db.Khu_Vuc.OrderBy(k => k.Ten_KV).ToList();
            ViewBag.Search = search;
            ViewBag.SelectedKv = idKv;
            ViewBag.SelectedStatus = status;

            return View(vm);
        }

        public ActionResult QuanLyKhachThue(string search, string status)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var query = db.Hop_Dong.Where(h => h.Phong_Tro.ID_TK == maTK);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(h => h.Khach_Thue.Ho_Ten.Contains(search) ||
                                         h.Khach_Thue.SDT.Contains(search) ||
                                         h.Khach_Thue.CCCD.Contains(search) ||
                                         h.Phong_Tro.Ten_Phong.Contains(search));
            }

            var rawList = query.OrderByDescending(h => h.ID_Hop_Dong).ToList();

            var listKhach = rawList.Select(h => new KhachThueItem
            {
                ID_Khach_Thue = h.ID_Khach_Thue,
                HoTen = h.Khach_Thue != null ? h.Khach_Thue.Ho_Ten : "",
                CCCD = h.Khach_Thue != null ? h.Khach_Thue.CCCD : "",
                SDT = h.Khach_Thue != null ? h.Khach_Thue.SDT : "",
                NgaySinh = h.Khach_Thue != null ? h.Khach_Thue.Ngay_Sinh : null,
                GioiTinh = h.Khach_Thue != null ? h.Khach_Thue.Gioi_Tinh : "",
                DiaChi = h.Khach_Thue != null ? h.Khach_Thue.Dia_Chi : "",
                TenPhong = h.Phong_Tro != null ? h.Phong_Tro.Ten_Phong : "",
                ID_HopDong = h.ID_Hop_Dong,
                NgayBatDau = h.Ngay_Bat_Dau,
                NgayKetThuc = h.Ngay_Ket_Thuc,
                GiaThue = h.Gia_Thue,
                HopDongHieuLuc = h.Trang_Thai
            }).ToList();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    listKhach = listKhach.Where(k => k.HopDongHieuLuc).ToList();
                else if (status == "expired")
                    listKhach = listKhach.Where(k => !k.HopDongHieuLuc).ToList();
            }

            var vm = new QuanLyKhachThueVM
            {
                TongKhach = rawList.Select(h => h.ID_Khach_Thue).Distinct().Count(),
                KhachDangThue = rawList.Where(h => h.Trang_Thai).Select(h => h.ID_Khach_Thue).Distinct().Count(),
                KhachDaKetThuc = rawList.Where(h => !h.Trang_Thai).Select(h => h.ID_Khach_Thue).Distinct().Count(),
                DanhSachKhach = listKhach
            };

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(vm);
        }

        public ActionResult QuanLyDienNuoc(int? thang, int? nam, bool? trangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var baseQuery = db.Hoa_Don_Dien_Nuoc.Where(h => h.Hop_Dong.Phong_Tro.ID_TK == maTK);

            var danhSachNam = baseQuery.Select(h => h.Nam).Distinct().OrderByDescending(x => x).ToList();
            if (!danhSachNam.Any())
            {
                danhSachNam.Add(DateTime.Now.Year);
            }

            var query = baseQuery;
            if (nam.HasValue && nam.Value > 0)
            {
                query = query.Where(h => h.Nam == nam.Value);
            }
            if (thang.HasValue && thang.Value > 0)
            {
                query = query.Where(h => h.Thang == thang.Value);
            }
            if (trangThai.HasValue)
            {
                query = query.Where(h => h.Trang_Thai == trangThai.Value);
            }

            var listHoaDon = query
                .OrderByDescending(h => h.Nam)
                .ThenByDescending(h => h.Thang)
                .ThenByDescending(h => h.ID_Hoa_Don)
                .Select(h => new HoaDonChiTietItem
                {
                    ID_Hoa_Don = h.ID_Hoa_Don,
                    TenPhong = h.Hop_Dong.Phong_Tro.Ten_Phong,
                    TenKhach = h.Hop_Dong.Khach_Thue.Ho_Ten,
                    SDT = h.Hop_Dong.Khach_Thue.SDT,
                    Thang = h.Thang,
                    Nam = h.Nam,
                    SoDien = h.So_Dien,
                    TienDien = h.Tien_Dien,
                    SoNuoc = h.So_Nuoc,
                    TienNuoc = h.Tien_Nuoc,
                    TienPhong = h.Tien_Phong,
                    TienWifi = h.Tien_Wifi,
                    TongTien = h.Tong_Tien,
                    NgayLap = h.Ngay_Lap,
                    HanThanhToan = h.Han_Thanh_Toan,
                    TrangThai = h.Trang_Thai
                })
                .ToList();

            var vm = new QuanLyDienNuocVM
            {
                TongTienDien = listHoaDon.Sum(h => h.TienDien),
                TongTienNuoc = listHoaDon.Sum(h => h.TienNuoc),
                SoHoaDonDaThu = listHoaDon.Count(h => h.TrangThai),
                SoHoaDonChuaThu = listHoaDon.Count(h => !h.TrangThai),
                TongTienDaThu = listHoaDon.Where(h => h.TrangThai).Sum(h => h.TongTien),
                TongTienChuaThu = listHoaDon.Where(h => !h.TrangThai).Sum(h => h.TongTien),
                SelectedThang = thang,
                SelectedNam = nam,
                SelectedTrangThai = trangThai,
                DanhSachNam = danhSachNam,
                DanhSachHoaDon = listHoaDon
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult CapNhatTrangThaiHoaDon(int id)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var hd = db.Hoa_Don_Dien_Nuoc.FirstOrDefault(x => x.ID_Hoa_Don == id && x.Hop_Dong.Phong_Tro.ID_TK == maTK);
            if (hd != null)
            {
                hd.Trang_Thai = !hd.Trang_Thai;
                db.SaveChanges();
                return Json(new { success = true, trangThai = hd.Trang_Thai });
            }
            return Json(new { success = false });
        }

        public ActionResult QuanLyHopDong(string search, string status)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var query = db.Hop_Dong.Where(h => h.Phong_Tro.ID_TK == maTK);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(h => h.Phong_Tro.Ten_Phong.Contains(search) ||
                                         h.Khach_Thue.Ho_Ten.Contains(search) ||
                                         h.Khach_Thue.SDT.Contains(search) ||
                                         h.Khach_Thue.CCCD.Contains(search));
            }

            var rawList = query.OrderByDescending(h => h.ID_Hop_Dong).ToList();

            var listHopDong = rawList.Select(h => new HopDongItem
            {
                ID_Hop_Dong = h.ID_Hop_Dong,
                ID_Phong_Tro = h.ID_Phong_Tro,
                TenPhong = h.Phong_Tro != null ? h.Phong_Tro.Ten_Phong : "",
                ID_Khach_Thue = h.ID_Khach_Thue,
                TenKhach = h.Khach_Thue != null ? h.Khach_Thue.Ho_Ten : "",
                SDT = h.Khach_Thue != null ? h.Khach_Thue.SDT : "",
                CCCD = h.Khach_Thue != null ? h.Khach_Thue.CCCD : "",
                NgayKy = h.Ngay_Ky,
                NgayBatDau = h.Ngay_Bat_Dau,
                NgayKetThuc = h.Ngay_Ket_Thuc,
                GiaThue = h.Gia_Thue,
                KyThanhToan = h.Ky_Thanh_Toan,
                TrangThai = h.Trang_Thai
            }).ToList();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    listHopDong = listHopDong.Where(h => h.TrangThai).ToList();
                else if (status == "expired")
                    listHopDong = listHopDong.Where(h => !h.TrangThai).ToList();
            }

            var vm = new QuanLyHopDongVM
            {
                TongHopDong = rawList.Count,
                HopDongHieuLuc = rawList.Count(h => h.Trang_Thai),
                HopDongHetHan = rawList.Count(h => !h.Trang_Thai),
                TongGiaTriThue = rawList.Where(h => h.Trang_Thai).Sum(h => h.Gia_Thue),
                DanhSachHopDong = listHopDong
            };

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(vm);
        }

        }
    }