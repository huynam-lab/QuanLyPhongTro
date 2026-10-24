using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyPhongTro.Areas.Host.Controllers
{
    public class BaseController : Controller
    {

        protected DaTa_Phong_TroEntities db = new DaTa_Phong_TroEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["UserID"] == null)
            {
                var hostUser = db.Tai_Khoan.FirstOrDefault(x => x.ID_Tk  == 2);
                if (hostUser != null)
                {
                    Session["UserID"] = hostUser.ID_Tk;
                    Session["UserName"] = hostUser.Name;
                    Session["SDT"] = hostUser.SDT;
                    Session["PhanQuyen"] = hostUser.ID_Phan_Quyen;
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
            if (maTK > 0)
                {
                var user = db.Tai_Khoan
                    .Where(x => x.ID_Tk == maTK)
                    .Select(x => new { x.Name, x.Avata, x.SDT })
                    .FirstOrDefault();
                var Admin = db.Tai_Khoan
                   .Where(x => x.ID_Tk == 1)
                   .Select(x => new { x.Name, x.SDT })
                   .FirstOrDefault();
                ViewBag.AdminName = Admin?.Name ?? "";
                ViewBag.AdminSDT = Admin?.SDT ?? "";
                ViewBag.Name = user?.Name ?? "";
                ViewBag.SDT = user?.SDT ?? "";
                ViewBag.Avata = user?.Avata ?? "";
                }

            base.OnActionExecuting(filterContext);
            }
        }
}