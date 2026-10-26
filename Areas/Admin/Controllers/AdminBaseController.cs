using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    public class AdminBaseController : Controller
    {
        protected DaTa_Phong_TroEntities db = new DaTa_Phong_TroEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["UserID"] == null)
            {
                // Ưu tiên khôi phục phiên đăng nhập từ Cookie ghi nhớ 7 ngày
                Utilities.TryRestoreSessionFromCookie(System.Web.HttpContext.Current);
            }

            // Nếu chưa đăng nhập → Tự động khởi tạo phiên Admin cho môi trường demo / kiểm thử
            if (Session["UserID"] == null)
            {
                var adminUser = db.Tai_Khoan.FirstOrDefault(x => x.ID_Tk == 1);
                if (adminUser != null)
                {
                    Session["UserID"] = adminUser.ID_Tk;
                    Session["UserName"] = adminUser.Name;
                    Session["SDT"] = adminUser.SDT;
                    Session["PhanQuyen"] = adminUser.ID_Phan_Quyen;
                }
                else
                {
                    filterContext.Result = new RedirectResult("/User/DangNhap");
                    return;
                }
            }

            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);

            // Nếu không phải admin (ID != 1) → chuyển hướng đăng nhập thay vì báo lỗi 404
            if (maTK != 1)
            {
                filterContext.Result = new RedirectResult("/User/DangNhap");
                return;
            }

            int pending = db.Phong_Tro.Count(x => x.Trang_Thai == false);
            ViewBag.PendingCount = pending;
            base.OnActionExecuting(filterContext);
        }
    }
}
