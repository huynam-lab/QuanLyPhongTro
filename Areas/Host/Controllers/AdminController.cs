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
        // Äá»“ng bá»™ tráº¡ng thÃ¡i bÃ i Ä‘Äƒng: Khi phÃ²ng cÃ³ ngÆ°á»i á»Ÿ (há»£p Ä‘á»“ng Ä‘ang hiá»‡u lá»±c) -> BÃ i Ä‘Äƒng chuyá»ƒn sang KhÃ´ng hiá»ƒn thá»‹ (Trang_Thai = false)
        private void SyncPhongTroVisibility(int idPhongTro)
        {
            var phong = db.Phong_Tro.FirstOrDefault(p => p.ID_Phong_Tro == idPhongTro);
            if (phong != null)
            {
                bool coNguoiO = db.Hop_Dong.Any(h => h.ID_Phong_Tro == idPhongTro && h.Trang_Thai == true);
                if (coNguoiO && phong.Trang_Thai == true)
                {
                    phong.Trang_Thai = false;
                    db.SaveChanges();
                }
            }
        }

        private void SyncAllHostRoomsVisibility(int maTK)
        {
            var roomsWithActiveContract = db.Phong_Tro
                .Where(p => p.ID_TK == maTK && p.Trang_Thai == true && p.Hop_Dong.Any(h => h.Trang_Thai))
                .ToList();
            if (roomsWithActiveContract.Any())
            {
                foreach (var r in roomsWithActiveContract)
                {
                    r.Trang_Thai = false;
                }
                db.SaveChanges();
            }
        }

        public ActionResult DSTinDang()
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            SyncAllHostRoomsVisibility(maTK);

            var listPhong = db.Phong_Tro
                .Where(x => x.ID_TK == maTK)
                .OrderByDescending(x => x.ID_Phong_Tro)
                .Select(x => new PhongTroListVM
                {
                    ID_Phong_Tro = x.ID_Phong_Tro,
                    Ten_Phong = x.Ten_Phong,
                    Ngay_Dang = x.Ngay_Dang,
                    Trang_Thai = x.Trang_Thai,
                    DangCoKhachThue = x.Hop_Dong.Any(h => h.Trang_Thai)
                })
                .ToList();

            var vm = new ChuNha
            {
                PhongTros = listPhong
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult ToggleHienThiTin(int id)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var phong = db.Phong_Tro.FirstOrDefault(x => x.ID_Phong_Tro == id && x.ID_TK == maTK);
            if (phong == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y bÃ i Ä‘Äƒng." });

            bool coNguoiO = db.Hop_Dong.Any(h => h.ID_Phong_Tro == id && h.Trang_Thai);
            if (coNguoiO)
            {
                phong.Trang_Thai = false;
                db.SaveChanges();
                return Json(new { success = false, message = "PhÃ²ng Ä‘ang cÃ³ ngÆ°á»i á»Ÿ nÃªn bÃ i Ä‘Äƒng báº¯t buá»™c á»Ÿ tráº¡ng thÃ¡i KhÃ´ng hiá»ƒn thá»‹!" });
            }

            phong.Trang_Thai = !(phong.Trang_Thai ?? false);
            db.SaveChanges();
            return Json(new { success = true, trangThai = phong.Trang_Thai, message = (phong.Trang_Thai == true ? "ÄÃ£ báº­t hiá»ƒn thá»‹ bÃ i Ä‘Äƒng!" : "ÄÃ£ chuyá»ƒn bÃ i Ä‘Äƒng sang tráº¡ng thÃ¡i KhÃ´ng hiá»ƒn thá»‹!") });
        }

        private void DeletePhongTroCascade(int id)
        {
            var phong = db.Phong_Tro.FirstOrDefault(x => x.ID_Phong_Tro == id);
            if (phong == null) return;

            var hopDongs = db.Hop_Dong.Where(h => h.ID_Phong_Tro == id).ToList();
            foreach (var hd in hopDongs)
            {
                var hoaDons = db.Hoa_Don_Dien_Nuoc.Where(b => b.ID_Hop_Dong == hd.ID_Hop_Dong).ToList();
                db.Hoa_Don_Dien_Nuoc.RemoveRange(hoaDons);
            }
            db.Hop_Dong.RemoveRange(hopDongs);

            var yeuThichs = db.Yeu_Thich.Where(y => y.ID_Phong_Tro == id).ToList();
            db.Yeu_Thich.RemoveRange(yeuThichs);

            var imgs = db.Hinh_Anh.Where(x => x.ID_Phong_Tro == id).ToList();
            foreach (var img in imgs)
            {
                var path = Server.MapPath("~/Kho/Img/" + img.Url_Anh);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            db.Hinh_Anh.RemoveRange(imgs);

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

            db.Phong_Tro.Remove(phong);
            db.SaveChanges();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePhongTro(int id)
        {
            DeletePhongTroCascade(id);
            TempData["SuccessMessage"] = "XÃ³a phÃ²ng trá» thÃ nh cÃ´ng";
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

        // =========================================================================
        // 1. QUáº¢N LÃ PHÃ’NG TRá»Œ (DANH SÃCH + THÃŠM + Sá»¬A + XÃ“A)
        // =========================================================================
        public ActionResult QuanLyPhongTro(string search, int? idKv, string status)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            // Tá»± Ä‘á»™ng chuyá»ƒn bÃ i Ä‘Äƒng cá»§a cÃ¡c phÃ²ng Ä‘ang cÃ³ ngÆ°á»i á»Ÿ sang KhÃ´ng hiá»ƒn thá»‹
            SyncAllHostRoomsVisibility(maTK);

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
                    ID_KV = p.ID_KV,
                    ID_CD = p.ID_CD,
                    TenPhong = p.Ten_Phong,
                    TenKhuVuc = p.Khu_Vuc != null ? p.Khu_Vuc.Ten_KV : "",
                    DiaChi = p.Dia_Chi,
                    MoTa = p.Mo_Ta != null ? p.Mo_Ta.Replace("##split##", "\n") : "",
                    DienTich = p.Dien_Tich,
                    GiaThue = p.Gia_Ca,
                    NgayDang = p.Ngay_Dang,
                    NgayHetHan = p.Ngay_Het_Han,
                    TrangThaiDuyet = hdHienTai != null ? false : (p.Trang_Thai ?? false),
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

        [HttpPost]
        public ActionResult ThemPhongNhanh(string TenPhong, int? ID_KV, int? ID_CD, string DiaChi, double? DienTich, decimal? GiaThue, string MoTa)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            if (string.IsNullOrWhiteSpace(TenPhong))
                return Json(new { success = false, message = "Vui lÃ²ng nháº­p tÃªn phÃ²ng trá»!" });

            var firstLoaiTin = db.Loai_Tin.Select(l => (int?)l.ID_LoaiTin).FirstOrDefault() ?? 1;
            var pt = new Phong_Tro
            {
                ID_TK = maTK,
                Ten_Phong = TenPhong.Trim(),
                ID_KV = ID_KV ?? db.Khu_Vuc.Select(k => (int?)k.ID_KV).FirstOrDefault(),
                ID_CD = ID_CD ?? 4,
                ID_LoaiTin = firstLoaiTin,
                Dia_Chi = DiaChi ?? "",
                Dien_Tich = DienTich ?? 20,
                Gia_Ca = GiaThue ?? 0,
                Mo_Ta = (MoTa ?? "").Replace("\r\n", "##split##").Replace("\n", "##split##"),
                Ngay_Dang = DateTime.Now,
                Ngay_Het_Han = DateTime.Now.AddDays(30),
                Gia_Duyet = 0,
                Trang_Thai = true
            };
            db.Phong_Tro.Add(pt);
            db.SaveChanges();

            db.Noi_Bat.Add(new Noi_Bat
            {
                ID_Phong_Tro = pt.ID_Phong_Tro,
                Day_du_noi_that = true,
                Gio_giac_tu_do = true
            });
            db.SaveChanges();

            return Json(new { success = true, message = "ÄÃ£ thÃªm phÃ²ng trá» má»›i thÃ nh cÃ´ng!" });
        }

        [HttpPost]
        public ActionResult SuaPhongNhanh(int ID_Phong_Tro, string TenPhong, int? ID_KV, string DiaChi, double? DienTich, decimal? GiaThue, bool? TrangThaiHienThi)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var phong = db.Phong_Tro.FirstOrDefault(p => p.ID_Phong_Tro == ID_Phong_Tro && p.ID_TK == maTK);
            if (phong == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y phÃ²ng trá» cáº§n sá»­a!" });

            phong.Ten_Phong = string.IsNullOrWhiteSpace(TenPhong) ? phong.Ten_Phong : TenPhong.Trim();
            if (ID_KV.HasValue && ID_KV.Value > 0) phong.ID_KV = ID_KV.Value;
            phong.Dia_Chi = DiaChi ?? phong.Dia_Chi;
            if (DienTich.HasValue) phong.Dien_Tich = DienTich.Value;
            if (GiaThue.HasValue) phong.Gia_Ca = GiaThue.Value;

            bool dangCoNguoiO = db.Hop_Dong.Any(h => h.ID_Phong_Tro == ID_Phong_Tro && h.Trang_Thai);
            if (dangCoNguoiO)
            {
                // Khi Ä‘ang cÃ³ ngÆ°á»i á»Ÿ thÃ¬ bÃ i Ä‘Äƒng báº¯t buá»™c khÃ´ng hiá»ƒn thá»‹
                phong.Trang_Thai = false;
            }
            else if (TrangThaiHienThi.HasValue)
            {
                phong.Trang_Thai = TrangThaiHienThi.Value;
            }

            db.SaveChanges();
            return Json(new { success = true, message = "Cáº­p nháº­t thÃ´ng tin phÃ²ng trá» thÃ nh cÃ´ng!" });
        }

        [HttpPost]
        public ActionResult XoaPhongTroAjax(int id)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var phong = db.Phong_Tro.FirstOrDefault(p => p.ID_Phong_Tro == id && p.ID_TK == maTK);
            if (phong == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y phÃ²ng trá» cáº§n xÃ³a!" });

            DeletePhongTroCascade(id);
            return Json(new { success = true, message = "ÄÃ£ xÃ³a phÃ²ng trá» vÃ  dá»¯ liá»‡u liÃªn quan thÃ nh cÃ´ng!" });
        }

        // =========================================================================
        // 2. QUáº¢N LÃ KHÃCH THUÃŠ (DANH SÃCH + THÃŠM + Sá»¬A + XÃ“A)
        // =========================================================================
        public ActionResult QuanLyKhachThue(string search, string status)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var khachListRaw = db.Khach_Thue
                .Where(k => k.ID_TK == maTK || k.Hop_Dong.Any(h => h.Phong_Tro.ID_TK == maTK))
                .OrderByDescending(k => k.ID_Khach_Thue)
                .ToList();

            var listKhach = new List<KhachThueItem>();
            foreach (var k in khachListRaw)
            {
                var hd = k.Hop_Dong
                    .Where(h => h.Phong_Tro != null && h.Phong_Tro.ID_TK == maTK)
                    .OrderByDescending(h => h.Trang_Thai)
                    .ThenByDescending(h => h.ID_Hop_Dong)
                    .FirstOrDefault();

                listKhach.Add(new KhachThueItem
                {
                    ID_Khach_Thue = k.ID_Khach_Thue,
                    HoTen = k.Ho_Ten ?? "",
                    CCCD = k.CCCD ?? "",
                    SDT = k.SDT ?? "",
                    NgaySinh = k.Ngay_Sinh,
                    GioiTinh = k.Gioi_Tinh ?? "Nam",
                    DiaChi = k.Dia_Chi ?? "",
                    TenPhong = hd != null && hd.Phong_Tro != null ? hd.Phong_Tro.Ten_Phong : "ChÆ°a xáº¿p phÃ²ng",
                    ID_HopDong = hd != null ? hd.ID_Hop_Dong : 0,
                    NgayBatDau = hd != null ? hd.Ngay_Bat_Dau : k.Ngay_Tao,
                    NgayKetThuc = hd != null ? hd.Ngay_Ket_Thuc : null,
                    GiaThue = hd != null ? hd.Gia_Thue : 0,
                    HopDongHieuLuc = hd != null ? hd.Trang_Thai : k.Trang_Thai
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower();
                listKhach = listKhach.Where(k =>
                    (k.HoTen != null && k.HoTen.ToLower().Contains(kw)) ||
                    (k.SDT != null && k.SDT.ToLower().Contains(kw)) ||
                    (k.CCCD != null && k.CCCD.ToLower().Contains(kw)) ||
                    (k.TenPhong != null && k.TenPhong.ToLower().Contains(kw))
                ).ToList();
            }

            int tongKhach = listKhach.Count;
            int khachDangThue = listKhach.Count(k => k.HopDongHieuLuc);
            int khachDaKetThuc = listKhach.Count(k => !k.HopDongHieuLuc);

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    listKhach = listKhach.Where(k => k.HopDongHieuLuc).ToList();
                else if (status == "expired")
                    listKhach = listKhach.Where(k => !k.HopDongHieuLuc).ToList();
            }

            var vm = new QuanLyKhachThueVM
            {
                TongKhach = tongKhach,
                KhachDangThue = khachDangThue,
                KhachDaKetThuc = khachDaKetThuc,
                DanhSachKhach = listKhach
            };

            ViewBag.DanhSachPhong = db.Phong_Tro.Where(p => p.ID_TK == maTK).OrderByDescending(p => p.ID_Phong_Tro).ToList();
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(vm);
        }

        [HttpPost]
        public ActionResult ThemKhachThue(string HoTen, string SDT, string CCCD, DateTime? NgaySinh, string GioiTinh, string DiaChi, int? ID_Phong_Tro, decimal? GiaThue, DateTime? NgayBatDau, DateTime? NgayKetThuc)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            if (string.IsNullOrWhiteSpace(HoTen))
                return Json(new { success = false, message = "Vui lÃ²ng nháº­p há» tÃªn khÃ¡ch thuÃª!" });

            var kt = new Khach_Thue
            {
                ID_TK = maTK,
                Ho_Ten = HoTen.Trim(),
                SDT = SDT ?? "",
                CCCD = CCCD ?? "",
                Ngay_Sinh = NgaySinh,
                Gioi_Tinh = string.IsNullOrWhiteSpace(GioiTinh) ? "Nam" : GioiTinh,
                Dia_Chi = DiaChi ?? "",
                Ngay_Tao = DateTime.Now,
                Trang_Thai = true
            };
            db.Khach_Thue.Add(kt);
            db.SaveChanges();

            // Náº¿u chá»n phÃ²ng thuÃª luÃ´n thÃ¬ tá»± Ä‘á»™ng láº­p há»£p Ä‘á»“ng vÃ  áº©n bÃ i Ä‘Äƒng phÃ²ng
            if (ID_Phong_Tro.HasValue && ID_Phong_Tro.Value > 0)
            {
                var phong = db.Phong_Tro.FirstOrDefault(p => p.ID_Phong_Tro == ID_Phong_Tro.Value && p.ID_TK == maTK);
                if (phong != null)
                {
                    var hd = new Hop_Dong
                    {
                        ID_Phong_Tro = phong.ID_Phong_Tro,
                        ID_Khach_Thue = kt.ID_Khach_Thue,
                        Ngay_Ky = DateTime.Now.Date,
                        Ngay_Bat_Dau = NgayBatDau ?? DateTime.Now.Date,
                        Ngay_Ket_Thuc = NgayKetThuc,
                        Gia_Thue = GiaThue ?? (phong.Gia_Ca ?? 0),
                        Ky_Thanh_Toan = 1,
                        Trang_Thai = true
                    };
                    db.Hop_Dong.Add(hd);
                    phong.Trang_Thai = false; // CÃ³ ngÆ°á»i á»Ÿ -> KhÃ´ng hiá»ƒn thá»‹ bÃ i Ä‘Äƒng
                    db.SaveChanges();
                }
            }

            return Json(new { success = true, message = "ÄÃ£ thÃªm khÃ¡ch thuÃª thÃ nh cÃ´ng!" });
        }

        [HttpPost]
        public ActionResult SuaKhachThue(int ID_Khach_Thue, string HoTen, string SDT, string CCCD, DateTime? NgaySinh, string GioiTinh, string DiaChi, bool? TrangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var kt = db.Khach_Thue.FirstOrDefault(k => k.ID_Khach_Thue == ID_Khach_Thue && (k.ID_TK == maTK || k.Hop_Dong.Any(h => h.Phong_Tro.ID_TK == maTK)));
            if (kt == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y khÃ¡ch thuÃª!" });

            if (!string.IsNullOrWhiteSpace(HoTen)) kt.Ho_Ten = HoTen.Trim();
            kt.SDT = SDT ?? kt.SDT;
            kt.CCCD = CCCD ?? kt.CCCD;
            kt.Ngay_Sinh = NgaySinh;
            kt.Gioi_Tinh = GioiTinh ?? kt.Gioi_Tinh;
            kt.Dia_Chi = DiaChi ?? kt.Dia_Chi;
            if (TrangThai.HasValue) kt.Trang_Thai = TrangThai.Value;

            db.SaveChanges();
            return Json(new { success = true, message = "ÄÃ£ cáº­p nháº­t thÃ´ng tin khÃ¡ch thuÃª!" });
        }

        [HttpPost]
        public ActionResult XoaKhachThue(int id)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var kt = db.Khach_Thue.FirstOrDefault(k => k.ID_Khach_Thue == id && (k.ID_TK == maTK || k.Hop_Dong.Any(h => h.Phong_Tro.ID_TK == maTK)));
            if (kt == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y khÃ¡ch thuÃª cáº§n xÃ³a!" });

            var hopDongs = db.Hop_Dong.Where(h => h.ID_Khach_Thue == id).ToList();
            foreach (var hd in hopDongs)
            {
                var hoaDons = db.Hoa_Don_Dien_Nuoc.Where(b => b.ID_Hop_Dong == hd.ID_Hop_Dong).ToList();
                db.Hoa_Don_Dien_Nuoc.RemoveRange(hoaDons);
            }
            db.Hop_Dong.RemoveRange(hopDongs);
            db.Khach_Thue.Remove(kt);
            db.SaveChanges();

            return Json(new { success = true, message = "ÄÃ£ xÃ³a khÃ¡ch thuÃª thÃ nh cÃ´ng!" });
        }

        // =========================================================================
        // 3. QUáº¢N LÃ ÄIá»†N NÆ¯á»šC (DANH SÃCH + THÃŠM + Sá»¬A + XÃ“A + THU TIá»€N)
        // =========================================================================
        public ActionResult QuanLyDienNuoc(int? thang, int? nam, bool? trangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var baseQuery = db.Hoa_Don_Dien_Nuoc.Where(h => h.Hop_Dong.Phong_Tro.ID_TK == maTK);

            var danhSachNam = baseQuery.Select(h => h.Nam).Distinct().OrderByDescending(x => x).ToList();
            if (!danhSachNam.Contains(DateTime.Now.Year))
            {
                danhSachNam.Insert(0, DateTime.Now.Year);
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
                    ID_Hop_Dong = h.ID_Hop_Dong,
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

            ViewBag.DanhSachHopDong = db.Hop_Dong
                .Include(h => h.Phong_Tro)
                .Include(h => h.Khach_Thue)
                .Where(h => h.Phong_Tro.ID_TK == maTK)
                .OrderByDescending(h => h.Trang_Thai)
                .ThenByDescending(h => h.ID_Hop_Dong)
                .ToList();

            return View(vm);
        }

        [HttpPost]
        public ActionResult ThemHoaDonDienNuoc(int ID_Hop_Dong, int Thang, int Nam, decimal SoDien, decimal TienDien, decimal SoNuoc, decimal TienNuoc, decimal TienPhong, decimal TienWifi, DateTime? HanThanhToan, bool TrangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var hd = db.Hop_Dong.FirstOrDefault(h => h.ID_Hop_Dong == ID_Hop_Dong && h.Phong_Tro.ID_TK == maTK);
            if (hd == null)
                return Json(new { success = false, message = "Vui lÃ²ng chá»n há»£p Ä‘á»“ng / phÃ²ng há»£p lá»‡!" });

            decimal tongTien = TienDien + TienNuoc + TienPhong + TienWifi;
            var bill = new Hoa_Don_Dien_Nuoc
            {
                ID_Hop_Dong = ID_Hop_Dong,
                Thang = Thang > 0 ? Thang : DateTime.Now.Month,
                Nam = Nam > 0 ? Nam : DateTime.Now.Year,
                So_Dien = SoDien,
                Tien_Dien = TienDien,
                So_Nuoc = SoNuoc,
                Tien_Nuoc = TienNuoc,
                Tien_Phong = TienPhong,
                Tien_Wifi = TienWifi,
                Tong_Tien = tongTien,
                Ngay_Lap = DateTime.Now.Date,
                Han_Thanh_Toan = HanThanhToan ?? DateTime.Now.Date.AddDays(5),
                Trang_Thai = TrangThai
            };
            db.Hoa_Don_Dien_Nuoc.Add(bill);
            db.SaveChanges();

            return Json(new { success = true, message = "ÄÃ£ thÃªm hÃ³a Ä‘Æ¡n Ä‘iá»‡n nÆ°á»›c thÃ nh cÃ´ng!" });
        }

        [HttpPost]
        public ActionResult SuaHoaDonDienNuoc(int ID_Hoa_Don, int ID_Hop_Dong, int Thang, int Nam, decimal SoDien, decimal TienDien, decimal SoNuoc, decimal TienNuoc, decimal TienPhong, decimal TienWifi, DateTime? HanThanhToan, bool TrangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var bill = db.Hoa_Don_Dien_Nuoc.FirstOrDefault(b => b.ID_Hoa_Don == ID_Hoa_Don && b.Hop_Dong.Phong_Tro.ID_TK == maTK);
            if (bill == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n cáº§n sá»­a!" });

            if (ID_Hop_Dong > 0) bill.ID_Hop_Dong = ID_Hop_Dong;
            if (Thang > 0) bill.Thang = Thang;
            if (Nam > 0) bill.Nam = Nam;
            bill.So_Dien = SoDien;
            bill.Tien_Dien = TienDien;
            bill.So_Nuoc = SoNuoc;
            bill.Tien_Nuoc = TienNuoc;
            bill.Tien_Phong = TienPhong;
            bill.Tien_Wifi = TienWifi;
            bill.Tong_Tien = TienDien + TienNuoc + TienPhong + TienWifi;
            bill.Han_Thanh_Toan = HanThanhToan;
            bill.Trang_Thai = TrangThai;

            db.SaveChanges();
            return Json(new { success = true, message = "ÄÃ£ cáº­p nháº­t hÃ³a Ä‘Æ¡n Ä‘iá»‡n nÆ°á»›c thÃ nh cÃ´ng!" });
        }

        [HttpPost]
        public ActionResult XoaHoaDonDienNuoc(int id)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var bill = db.Hoa_Don_Dien_Nuoc.FirstOrDefault(b => b.ID_Hoa_Don == id && b.Hop_Dong.Phong_Tro.ID_TK == maTK);
            if (bill == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n cáº§n xÃ³a!" });

            db.Hoa_Don_Dien_Nuoc.Remove(bill);
            db.SaveChanges();
            return Json(new { success = true, message = "ÄÃ£ xÃ³a hÃ³a Ä‘Æ¡n Ä‘iá»‡n nÆ°á»›c thÃ nh cÃ´ng!" });
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

        // =========================================================================
        // 4. QUáº¢N LÃ Há»¢P Äá»’NG (DANH SÃCH + THÃŠM + Sá»¬A + XÃ“A + Äá»’NG Bá»˜ áº¨N BÃ€I ÄÄ‚NG)
        // =========================================================================
        public ActionResult QuanLyHopDong(string search, string status)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            SyncAllHostRoomsVisibility(maTK);

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

            ViewBag.DanhSachPhong = db.Phong_Tro.Where(p => p.ID_TK == maTK).OrderByDescending(p => p.ID_Phong_Tro).ToList();
            ViewBag.DanhSachKhach = db.Khach_Thue.Where(k => k.ID_TK == maTK || k.Hop_Dong.Any(h => h.Phong_Tro.ID_TK == maTK)).OrderByDescending(k => k.ID_Khach_Thue).ToList();
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(vm);
        }

        [HttpPost]
        public ActionResult ThemHopDong(int ID_Phong_Tro, int? ID_Khach_Thue, string TenKhachMoi, string SDTMoi, string CCCDMoi, string DiaChiMoi, DateTime? NgayKy, DateTime? NgayBatDau, DateTime? NgayKetThuc, decimal GiaThue, int? KyThanhToan, bool TrangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var phong = db.Phong_Tro.FirstOrDefault(p => p.ID_Phong_Tro == ID_Phong_Tro && p.ID_TK == maTK);
            if (phong == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y phÃ²ng trá»!" });

            int khachId = ID_Khach_Thue ?? 0;
            if (khachId <= 0)
            {
                if (string.IsNullOrWhiteSpace(TenKhachMoi))
                    return Json(new { success = false, message = "Vui lÃ²ng chá»n khÃ¡ch thuÃª hoáº·c nháº­p tÃªn khÃ¡ch thuÃª má»›i!" });

                var newKhach = new Khach_Thue
                {
                    ID_TK = maTK,
                    Ho_Ten = TenKhachMoi.Trim(),
                    SDT = SDTMoi ?? "",
                    CCCD = CCCDMoi ?? "",
                    Dia_Chi = DiaChiMoi ?? "",
                    Gioi_Tinh = "Nam",
                    Ngay_Tao = DateTime.Now,
                    Trang_Thai = true
                };
                db.Khach_Thue.Add(newKhach);
                db.SaveChanges();
                khachId = newKhach.ID_Khach_Thue;
            }

            if (TrangThai)
            {
                // Káº¿t thÃºc cÃ¡c há»£p Ä‘á»“ng cÅ© Ä‘ang hiá»‡u lá»±c cá»§a cÃ¹ng phÃ²ng nÃ y (náº¿u cÃ³)
                var oldActive = db.Hop_Dong.Where(h => h.ID_Phong_Tro == ID_Phong_Tro && h.Trang_Thai).ToList();
                foreach (var old in oldActive)
                {
                    old.Trang_Thai = false;
                }
                // Khi cÃ³ ngÆ°á»i á»Ÿ -> bÃ i Ä‘Äƒng chuyá»ƒn sang tráº¡ng thÃ¡i KhÃ´ng hiá»ƒn thá»‹
                phong.Trang_Thai = false;
            }

            var hd = new Hop_Dong
            {
                ID_Phong_Tro = ID_Phong_Tro,
                ID_Khach_Thue = khachId,
                Ngay_Ky = NgayKy ?? DateTime.Now.Date,
                Ngay_Bat_Dau = NgayBatDau ?? DateTime.Now.Date,
                Ngay_Ket_Thuc = NgayKetThuc,
                Gia_Thue = GiaThue > 0 ? GiaThue : (phong.Gia_Ca ?? 0),
                Ky_Thanh_Toan = KyThanhToan ?? 1,
                Trang_Thai = TrangThai
            };
            db.Hop_Dong.Add(hd);
            db.SaveChanges();

            SyncPhongTroVisibility(ID_Phong_Tro);

            return Json(new { success = true, message = "ÄÃ£ táº¡o há»£p Ä‘á»“ng thÃ nh cÃ´ng! BÃ i Ä‘Äƒng cá»§a phÃ²ng Ä‘Ã£ tá»± Ä‘á»™ng chuyá»ƒn sang KhÃ´ng hiá»ƒn thá»‹." });
        }

        [HttpPost]
        public ActionResult SuaHopDong(int ID_Hop_Dong, int ID_Phong_Tro, string TenKhach, string SDT, string CCCD, DateTime? NgayKy, DateTime? NgayBatDau, DateTime? NgayKetThuc, decimal GiaThue, int? KyThanhToan, bool TrangThai)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var hd = db.Hop_Dong.FirstOrDefault(h => h.ID_Hop_Dong == ID_Hop_Dong && h.Phong_Tro.ID_TK == maTK);
            if (hd == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y há»£p Ä‘á»“ng cáº§n sá»­a!" });

            int oldPhongId = hd.ID_Phong_Tro;
            hd.ID_Phong_Tro = ID_Phong_Tro > 0 ? ID_Phong_Tro : hd.ID_Phong_Tro;
            if (NgayKy.HasValue) hd.Ngay_Ky = NgayKy.Value;
            if (NgayBatDau.HasValue) hd.Ngay_Bat_Dau = NgayBatDau.Value;
            hd.Ngay_Ket_Thuc = NgayKetThuc;
            hd.Gia_Thue = GiaThue;
            if (KyThanhToan.HasValue && KyThanhToan.Value > 0) hd.Ky_Thanh_Toan = KyThanhToan.Value;
            hd.Trang_Thai = TrangThai;

            if (hd.Khach_Thue != null)
            {
                if (!string.IsNullOrWhiteSpace(TenKhach)) hd.Khach_Thue.Ho_Ten = TenKhach.Trim();
                if (SDT != null) hd.Khach_Thue.SDT = SDT.Trim();
                if (CCCD != null) hd.Khach_Thue.CCCD = CCCD.Trim();
            }

            db.SaveChanges();

            // Äá»“ng bá»™ tráº¡ng thÃ¡i áº©n bÃ i Ä‘Äƒng khi phÃ²ng cÃ³ ngÆ°á»i á»Ÿ
            SyncPhongTroVisibility(oldPhongId);
            SyncPhongTroVisibility(hd.ID_Phong_Tro);

            return Json(new { success = true, message = "Cáº­p nháº­t há»£p Ä‘á»“ng thÃ nh cÃ´ng!" });
        }

        [HttpPost]
        public ActionResult XoaHopDong(int id)
        {
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            var hd = db.Hop_Dong.FirstOrDefault(h => h.ID_Hop_Dong == id && h.Phong_Tro.ID_TK == maTK);
            if (hd == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y há»£p Ä‘á»“ng cáº§n xÃ³a!" });

            int idPhong = hd.ID_Phong_Tro;
            var bills = db.Hoa_Don_Dien_Nuoc.Where(b => b.ID_Hop_Dong == id).ToList();
            db.Hoa_Don_Dien_Nuoc.RemoveRange(bills);
            db.Hop_Dong.Remove(hd);
            db.SaveChanges();

            SyncPhongTroVisibility(idPhong);

            return Json(new { success = true, message = "ÄÃ£ xÃ³a há»£p Ä‘á»“ng thÃ nh cÃ´ng!" });
        }

        }
    }
