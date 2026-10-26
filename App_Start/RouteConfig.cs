using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QuanLyPhongTro
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // RESTful AI API Endpoints (Sản phẩm #6 trong phiếu giao đề tài)
            routes.MapRoute(
                name: "AiForecastApi",
                url: "api/forecast",
                defaults: new { controller = "Chatbot", action = "ForecastApi" }
            );

            routes.MapRoute(
                name: "AiChatbotApi",
                url: "api/chatbot",
                defaults: new { controller = "Chatbot", action = "Ask" }
            );

            routes.MapRoute(
                name: "AiManagementStatsApi",
                url: "api/management-stats",
                defaults: new { controller = "Chatbot", action = "ManagementStatsApi" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "User", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
