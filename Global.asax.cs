using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QuanLyPhongTro
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            // Tự động kiểm tra và khôi phục phiên đăng nhập từ Cookie (hạn 7 ngày) trên mọi request
            if (HttpContext.Current != null && HttpContext.Current.Session != null)
            {
                Utilities.TryRestoreSessionFromCookie(HttpContext.Current);
            }
        }
    }
}
