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

        protected DaTa_Phong_TroEntities5 db = new DaTa_Phong_TroEntities5();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
            {
      
            int maTK = 0;
            if (Session["UserID"] != null)
                int.TryParse(Session["UserID"].ToString(), out maTK);
            if (Session["UserID"] == null)
                {
                filterContext.Result = new HttpNotFoundResult();
                return;
                }
            if (maTK > 0)
                {
                var user = db.Tai_Khoan
                    .Where(x => x.ID_TK == maTK)
                    .Select(x => new { x.Name, x.Avata, x.SDT })
                    .FirstOrDefault();
                var Admin = db.Tai_Khoan
                   .Where(x => x.ID_TK == 1)
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