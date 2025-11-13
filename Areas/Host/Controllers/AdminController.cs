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

            // 🏠 Tạo phòng trọ mới
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

            System.Diagnostics.Debug.WriteLine($"🆕 Phòng mới ID = {pt.ID_Phong_Tro}, Tiêu đề = {pt.Ten_Phong}");

            // 🧩 Lưu đặc điểm nổi bật
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


            /* 🖼️ LƯU NHIỀU ẢNH */
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
            /* 🎥 LƯU NHIỀU VIDEO */
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

                        db.Videos.Add(new Video
                            {
                            ID_Phong_Tro = pt.ID_Phong_Tro,
                            Url_Video = videoName
                            });

                        System.Diagnostics.Debug.WriteLine($"✅ Lưu video: {file.FileName} → {videoName}");
                        }
                    catch (Exception ex)
                        {
                        System.Diagnostics.Debug.WriteLine("❌ Lỗi lưu video: " + ex.ToString());
                        }
                    }
                }
            db.SaveChanges();
            TempData["SuccessMessage"] = "Vui lòng liên hệ quản trị viên để duyệt";
            return RedirectToAction("DSTinDang");

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
            var videos = db.Videos.Where(x => x.ID_Phong_Tro == id).ToList();
            foreach (var v in videos)
                {
                var path = Server.MapPath("~/Kho/Video/" + v.Url_Video);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                }
            db.Videos.RemoveRange(videos);
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
                .Where(x => x.ID_TK == maTK)
                .Select(x => new { x.Name, x.SDT, x.Avata })
                .FirstOrDefault();

            ViewBag.Name = user?.Name ?? "";
            ViewBag.SDT = user?.SDT ?? "";
            ViewBag.Avata = user?.Avata ?? "";

            // tạo viewmodel
            var vm = new ChuNha
                {
                HinhAnhs = db.Hinh_Anh.Where(x => x.ID_Phong_Tro == id).Select(x => x.Url_Anh).ToList(),
                Videos = db.Videos.Where(x => x.ID_Phong_Tro == id).Select(x => x.Url_Video).ToList(),
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

                    db.Videos.Add(new Video
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
            var tk = db.Tai_Khoan.FirstOrDefault(x => x.ID_TK == maTK);
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
            var tk = db.Tai_Khoan.FirstOrDefault(x => x.ID_TK == maTK);

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
            if (Session["MaTK"] != null)
                int.TryParse(Session["MaTK"].ToString(), out maTK);

            var tk = db.Tai_Khoan.FirstOrDefault(x => x.ID_TK == maTK);
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

        }
    }