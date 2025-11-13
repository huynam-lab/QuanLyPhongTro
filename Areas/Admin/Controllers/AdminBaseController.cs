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
        protected DaTa_Phong_TroEntities5 db = new DaTa_Phong_TroEntities5();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
            {
            // Nếu chưa đăng nhập → 404
            if (Session["UserID"] == null)
                {
                filterContext.Result = new HttpNotFoundResult();
                return;
                }

            int maTK;
            int.TryParse(Session["UserID"].ToString(), out maTK);

            // Nếu không phải admin (ID != 1) → 404
            if (maTK != 1)
                {
                filterContext.Result = new HttpNotFoundResult();
                return;
                }
            // Admin → cho đi tiếp
            base.OnActionExecuting(filterContext);
            }

        }
    }