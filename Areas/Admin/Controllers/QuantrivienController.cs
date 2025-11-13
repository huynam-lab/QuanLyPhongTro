using QuanLyPhongTro.Areas.Admin.AdminViewModels;
using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    public class QuantrivienController : AdminBaseController
    {
        // GET: Admin/Dashboard
        public ActionResult Dashboard()
            {
            ViewBag.Menu = "dashboard";

            // --- Tổng quan ---
            ViewBag.TotalUser = db.Tai_Khoan.Count();
            ViewBag.HetHan = db.Phong_Tro
             .Count(x => x.Trang_Thai == true &&
                         x.Ngay_Het_Han.HasValue &&
                            x.Ngay_Het_Han.Value < DateTime.Now);

            ViewBag.PhongHienThi = db.Phong_Tro.Count(x => x.Trang_Thai == true);
            ViewBag.PhongChoDuyet = db.Phong_Tro.Count(x => x.Trang_Thai == false);

            // --- Tin mới nhất ---
            var latest = db.Phong_Tro
                .OrderByDescending(x => x.Ngay_Dang)
                .Take(10)
                .ToList();

            // --- Biểu đồ 1: Số lượng bài đăng theo tháng ---
            var postsData = db.Phong_Tro
                .Where(p => p.Ngay_Dang.HasValue)
                .GroupBy(p => new { p.Ngay_Dang.Value.Year, p.Ngay_Dang.Value.Month })
                .Select(g => new
                    {
                    Thang = g.Key.Month,
                    Nam = g.Key.Year,
                    SoLuong = g.Count()
                    })
                .OrderBy(g => g.Nam).ThenBy(g => g.Thang)
                .ToList();

            ViewBag.PostsLabels = postsData.Select(x => $"{x.Thang}/{x.Nam}").ToArray();
            ViewBag.PostsCounts = postsData.Select(x => x.SoLuong).ToArray();

            // --- Biểu đồ 2: Số lượng tài khoản tạo mới theo tháng ---
            var usersData = db.Tai_Khoan
                .Where(u => u.Ngay_Tao.HasValue)
                .GroupBy(u => new { u.Ngay_Tao.Value.Year, u.Ngay_Tao.Value.Month })
                .Select(g => new
                    {
                    Thang = g.Key.Month,
                    Nam = g.Key.Year,
                    SoLuong = g.Count()
                    })
                .OrderBy(g => g.Nam).ThenBy(g => g.Thang)
                .ToList();

            ViewBag.UsersLabels = usersData.Select(x => $"{x.Thang}/{x.Nam}").ToArray();
            ViewBag.UsersCounts = usersData.Select(x => x.SoLuong).ToArray();

            return View(latest);
            }


        // GET: Admin/TaiKhoan
        public ActionResult TaiKhoan(int? role, string search)
            {
            ViewBag.Menu = "account";

            var query = db.Tai_Khoan.AsQueryable();

            if (role.HasValue)
                query = query.Where(x => x.ID_Phan_Quyen == role.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.Name.Contains(search) || x.SDT.Contains(search) || x.User_Name.Contains(search));

            ViewBag.Roles = db.Phan_Quyen.ToList();

            var list = query
                .OrderByDescending(x => x.ID_TK)
                .ToList();

            return View(list);
            }
        // Bật / tắt tài khoản
        [HttpPost]
        public ActionResult ToggleTaiKhoan(int id)
            {
            var tk = db.Tai_Khoan.Find(id);
            if (tk == null) return HttpNotFound();

            tk.Trang_Thai = !(tk.Trang_Thai ?? false);
            db.SaveChanges();

            return RedirectToAction("TaiKhoan");
            }

        // GET: Admin/PhongTro
        public ActionResult PhongTro(string status, string search)
            {
            ViewBag.Menu = "phong";
            var q = db.Phong_Tro.AsQueryable();

            // 🔍 Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
                {
                if (status == "hienthi")
                    {
                    q = q.Where(x => x.Trang_Thai == true &&
                                     (!x.Ngay_Het_Han.HasValue || x.Ngay_Het_Han.Value >= DateTime.Now));
                    }
                else if (status == "choduyet")
                    {
                    q = q.Where(x => x.Trang_Thai == false);
                    }
                else if (status == "hethang")
                    {
                    q = q.Where(x => x.Trang_Thai == true &&
                                     x.Ngay_Het_Han.HasValue &&
                                     x.Ngay_Het_Han.Value < DateTime.Now);
                    }
                }

            // 🔍 Lọc theo từ khóa
            if (!string.IsNullOrEmpty(search))
                {
                q = q.Where(x => x.Ten_Phong.Contains(search)
                              || x.ID_Phong_Tro.ToString().Contains(search));
                }

            // ✅ Sắp xếp mới nhất
            var list = q.OrderByDescending(x => x.Ngay_Dang).ToList();
            return View(list);
            }


        // duyệt tin
        [HttpPost]
        public ActionResult DuyetTin(int id)
            {
            var p = db.Phong_Tro.Find(id);
            if (p == null) return HttpNotFound();

            p.Trang_Thai = true;
            db.SaveChanges();

            return RedirectToAction("PhongTro");
            }

        // xóa tin
        [HttpPost]
        public ActionResult XoaTin(int id)
            {
            var p = db.Phong_Tro.Find(id);
            if (p == null) return HttpNotFound();

            // xóa phụ thuộc
            db.Hinh_Anh.RemoveRange(db.Hinh_Anh.Where(x => x.ID_Phong_Tro == id));
            db.Videos.RemoveRange(db.Videos.Where(x => x.ID_Phong_Tro == id));
            db.Noi_Bat.RemoveRange(db.Noi_Bat.Where(x => x.ID_Phong_Tro == id));
            db.Yeu_Thich.RemoveRange(db.Yeu_Thich.Where(x => x.ID_Phong_Tro == id));

            db.Phong_Tro.Remove(p);
            db.SaveChanges();

            return RedirectToAction("PhongTro");
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GiaHanTin(int id, int soNgay)
            {
            var tin = db.Phong_Tro.Find(id);
            if (tin != null)
                {
                var baseDate = (tin.Ngay_Het_Han.HasValue && tin.Ngay_Het_Han.Value > DateTime.Now)
                    ? tin.Ngay_Het_Han.Value
                    : DateTime.Now;

                tin.Ngay_Het_Han = baseDate.AddDays(soNgay);
                db.SaveChanges();
                }
            TempData["Message"] = $"Đã gia hạn tin #{id} thêm {soNgay} ngày.";
            return RedirectToAction("PhongTro");
            }





        [HttpGet]
        public ActionResult Catalogs(string tab = "khuvuc", int? filterLoaiTin = null)
            {
            ViewBag.ActiveTab = tab; // tab đang mở

            // Lấy danh sách loại tin
            var loaiTins = db.Loai_Tin.OrderBy(x => x.Ten_LoaiTin).ToList();

            // Nếu chưa chọn loại tin nào thì lấy loại tin đầu tiên làm mặc định
            if (filterLoaiTin == null && loaiTins.Any())
                {
                filterLoaiTin = loaiTins.First().ID_LoaiTin;
                }

            ViewBag.FilterLoaiTin = filterLoaiTin;

            // Tạo ViewModel
            var vm = new CatalogVM
                {
                KhuVucs = db.Khu_Vuc.OrderBy(x => x.Ten_KV).ToList(),
                LoaiTins = loaiTins,
                BangGias = db.Bang_Gia_Tin
                             .Where(b => b.ID_LoaiTin == filterLoaiTin)  // chỉ lấy giá của loại tin đang chọn
                             .OrderBy(x => x.Thoi_Gian)
                             .ToList(),
                NoiBats = db.Noi_Bat
                            .OrderByDescending(x => x.ID_Noi_Bat)
                            .Take(200)
                            .ToList(),
                 ChuDes = db.Chu_De.OrderBy(x => x.Ten_CD).ToList()
                };

            return View(vm);
            }


        // ============ KHU VỰC ============
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveKhuVuc(int? ID_KV, string Ten_KV, bool Trang_Thai = true)
            {
            if (string.IsNullOrWhiteSpace(Ten_KV))
                {
                TempData["Error"] = "Tên khu vực không được rỗng.";
                TempData["ActiveTab"] = "khuvuc";
                return RedirectToAction("Catalogs");
                }

            if (ID_KV.HasValue) // update
                {
                var kv = db.Khu_Vuc.Find(ID_KV.Value);
                if (kv == null) { TempData["Error"] = "Không tìm thấy khu vực."; }
                else { kv.Ten_KV = Ten_KV.Trim(); kv.Trang_Thai = Trang_Thai; db.SaveChanges(); TempData["Success"] = "Đã cập nhật khu vực."; }
                }
            else // create
                {
                db.Khu_Vuc.Add(new Khu_Vuc { Ten_KV = Ten_KV.Trim(), Trang_Thai = Trang_Thai });
                db.SaveChanges();
                TempData["Success"] = "Đã thêm khu vực.";
                }
            TempData["ActiveTab"] = "khuvuc";
            return RedirectToAction("Catalogs");
            }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteKhuVuc(int id)
            {
            var kv = db.Khu_Vuc.Find(id);
            if (kv == null) TempData["Error"] = "Không tìm thấy khu vực.";
            else { db.Khu_Vuc.Remove(kv); db.SaveChanges(); TempData["Success"] = "Đã xóa khu vực."; }
            TempData["ActiveTab"] = "khuvuc";
            return RedirectToAction("Catalogs");
            }

        // ============ LOẠI TIN ============
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveLoaiTin(int? ID_LoaiTin, string Ten_LoaiTin, int CapDo, string Mau_TieuDe, string KichThuoc,
                                        bool Tu_Dong_Duyet = false, bool Hien_Thi_Goi_Dien = true, bool Trang_Thai = true)
            {
            if (string.IsNullOrWhiteSpace(Ten_LoaiTin))
                {
                TempData["Error"] = "Tên loại tin không được rỗng.";
                TempData["ActiveTab"] = "loaitin";
                return RedirectToAction("Catalogs", new { tab = "loaitin" });
                }

            if (ID_LoaiTin.HasValue)
                {
                var lt = db.Loai_Tin.Find(ID_LoaiTin.Value);
                if (lt == null) TempData["Error"] = "Không tìm thấy loại tin.";
                else
                    {
                    lt.Ten_LoaiTin = Ten_LoaiTin.Trim();
                    lt.CapDo = (int)CapDo;
                    lt.Mau_TieuDe = Mau_TieuDe;
                    lt.KichThuoc = KichThuoc;
                    lt.Tu_Dong_Duyet = Tu_Dong_Duyet;
                    lt.Hien_Thi_Goi_Dien = Hien_Thi_Goi_Dien;
                    lt.Trang_Thai = Trang_Thai;
                    db.SaveChanges();
                    TempData["Success"] = "Đã cập nhật loại tin.";
                    }
                }
            else
                {
                db.Loai_Tin.Add(new Loai_Tin
                    {
                    Ten_LoaiTin = Ten_LoaiTin.Trim(),
                    CapDo = CapDo,
                    Mau_TieuDe = Mau_TieuDe,
                    KichThuoc = KichThuoc,
                    Tu_Dong_Duyet = Tu_Dong_Duyet,
                    Hien_Thi_Goi_Dien = Hien_Thi_Goi_Dien,
                    Trang_Thai = Trang_Thai
                    });
                db.SaveChanges();
                TempData["Success"] = "Đã thêm loại tin.";
                }

            TempData["ActiveTab"] = "loaitin";
            return RedirectToAction("Catalogs", new { tab = "loaitin" });
            }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteLoaiTin(int id)
            {
            var lt = db.Loai_Tin.Find(id);

            if (lt == null)
                {
                TempData["Error"] = "Không tìm thấy loại tin.";
                }
            else
                {
                // Kiểm tra loại tin này có đang được sử dụng trong Phòng_Trọ hay không
                bool hasPhongTro = db.Phong_Tro.Any(p => p.ID_LoaiTin == id);

                if (hasPhongTro)
                    {
                    TempData["Error"] = "Không thể xóa loại tin này vì đang có phòng trọ sử dụng!";
                    }
                else
                    {
                    db.Loai_Tin.Remove(lt);
                    db.SaveChanges();
                    TempData["Success"] = "Đã xóa loại tin thành công!";
                    }
                }

            TempData["ActiveTab"] = "loaitin";
            return RedirectToAction("Catalogs", new { tab = "loaitin" });
            }


        // ============ BẢNG GIÁ TIN ============
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveBangGia(int? ID_Gia, int ID_LoaiTin, string Thoi_Gian, decimal? Gia_Goc, decimal? Gia_Giam, int? Ty_Le_Giam, string Ghi_Chu)
            {
            if (!db.Loai_Tin.Any(x => x.ID_LoaiTin == ID_LoaiTin))
                {
                TempData["Error"] = "Loại tin không hợp lệ.";
                TempData["ActiveTab"] = "loaitin";
                return RedirectToAction("Catalogs", new { tab = "loaitin" });
                }

            if (ID_Gia.HasValue)
                {
                var g = db.Bang_Gia_Tin.Find(ID_Gia.Value);
                if (g == null) TempData["Error"] = "Không tìm thấy mục giá.";
                else
                    {
                    g.ID_LoaiTin = ID_LoaiTin;
                    g.Thoi_Gian = Thoi_Gian;
                    g.Gia_Goc = Gia_Goc;
                    g.Gia_Giam = Gia_Giam;
                    g.Ty_Le_Giam = (int)Ty_Le_Giam;
                    g.Ghi_Chu = Ghi_Chu;
                    db.SaveChanges();
                    TempData["Success"] = "Đã cập nhật bảng giá.";
                    }
                }
            else
                {
                db.Bang_Gia_Tin.Add(new Bang_Gia_Tin
                    {
                    ID_LoaiTin = ID_LoaiTin,
                    Thoi_Gian = Thoi_Gian,
                    Gia_Goc = Gia_Goc,
                    Gia_Giam = Gia_Giam,
                    Ty_Le_Giam = Ty_Le_Giam,
                    Ghi_Chu = Ghi_Chu
                    });
                db.SaveChanges();
                TempData["Success"] = "Đã thêm bảng giá.";
                }

            TempData["ActiveTab"] = "loaitin";
            return RedirectToAction("Catalogs", new { tab = "loaitin", filterLoaiTin = ID_LoaiTin });
            }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteBangGia(int id, int? lt)
            {
            var g = db.Bang_Gia_Tin.Find(id);

            if (g == null)
                {
                TempData["Error"] = "Không tìm thấy mục giá.";
                }
            else
                {
                // ✅ Kiểm tra xem loại tin này có đang được sử dụng không
                var loaiTin = db.Loai_Tin.FirstOrDefault(x => x.ID_LoaiTin == g.ID_LoaiTin);

                if (loaiTin == null)
                    {
                    TempData["Error"] = "Loại tin của bảng giá này không tồn tại.";
                    }
                else
                    {
                    // ✅ Nếu loại tin đang có phòng trọ sử dụng -> không cho xóa
                    bool hasPhongTro = db.Phong_Tro.Any(p => p.ID_LoaiTin == loaiTin.ID_LoaiTin);

                    if (hasPhongTro)
                        {
                        TempData["Error"] = "Không thể xóa bảng giá này vì loại tin đang được phòng trọ sử dụng!";
                        }
                    else
                        {
                        db.Bang_Gia_Tin.Remove(g);
                        db.SaveChanges();
                        TempData["Success"] = "Đã xóa bảng giá thành công!";
                        }
                    }
                }

            TempData["ActiveTab"] = "loaitin";
            return RedirectToAction("Catalogs", new { tab = "loaitin", filterLoaiTin = lt });
            }

        // ================== QUẢN LÝ CHỦ ĐỀ ==================
        [HttpGet]
        public ActionResult ChuDe()
            {
            ViewBag.Menu = "chude";
            var list = db.Chu_De.OrderBy(x => x.Ten_CD).ToList();
            return View(list);
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveChuDe(Chu_De model)
            {
            if (ModelState.IsValid)
                {
                if (model.ID_CD == 0)
                    {
                    db.Chu_De.Add(model);
                    }
                else
                    {
                    var cd = db.Chu_De.Find(model.ID_CD);
                    if (cd != null)
                        {
                        cd.Ten_CD = model.Ten_CD;
                        }
                    }
                db.SaveChanges();
                }

            TempData["Success"] = "Lưu chủ đề thành công!";
            return RedirectToAction("Catalogs", new { tab = "chude" });
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteChuDe(int id)
            {
            var cd = db.Chu_De.Find(id);
            if (cd != null)
                {
                // kiểm tra xem chủ đề này có phòng trọ nào không
                bool hasPhongTro = db.Phong_Tro.Any(p => p.ID_CD == id);

                if (hasPhongTro)
                    {
                    TempData["Error"] = "Không thể xóa chủ đề này vì đang có phòng trọ sử dụng!";
                    }
                else
                    {
                    db.Chu_De.Remove(cd);
                    db.SaveChanges();
                    TempData["Success"] = "Đã xóa chủ đề thành công!";
                    }
                }
            else
                {
                TempData["Error"] = "Không tìm thấy chủ đề cần xóa!";
                }

            // quay lại tab 'chude' trong trang Catalogs
            return RedirectToAction("Catalogs", new { tab = "chude" });
            }

        }
    }