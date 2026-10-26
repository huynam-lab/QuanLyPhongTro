using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using QuanLyPhongTro.Models;

public static class Utilities
{
    public const string REMEMBER_COOKIE_NAME = "QLPT_RememberAuth_7D";
    public const int REMEMBER_DAYS = 7;
    private const string COOKIE_SECRET = "QLPT_Secret_Key_2026_7Days";

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return null;
        }

        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// Lưu phiên đăng nhập vào Session và tạo Cookie ghi nhớ đăng nhập hạn 7 ngày
    /// </summary>
    public static void SetLoginSessionAndCookie(HttpResponseBase response, HttpSessionStateBase session, Tai_Khoan account, bool rememberMe = true)
    {
        if (account == null) return;

        if (session != null)
        {
            session.Timeout = REMEMBER_DAYS * 24 * 60; // 7 ngày (10080 phút)
            session["UserID"] = account.ID_Tk;
            session["UserName"] = account.Name;
            session["SDT"] = account.SDT;
            session["Avatar"] = account.Avata;
            session["PhanQuyen"] = account.ID_Phan_Quyen;
        }

        if (response != null)
        {
            if (rememberMe)
            {
                long expTicks = DateTime.UtcNow.AddDays(REMEMBER_DAYS).Ticks;
                string rawSig = $"{account.ID_Tk}|{account.Pass ?? ""}|{expTicks}|{COOKIE_SECRET}";
                string sig = HashPassword(rawSig);

                HttpCookie cookie = new HttpCookie(REMEMBER_COOKIE_NAME);
                cookie.Values["uid"] = account.ID_Tk.ToString();
                cookie.Values["exp"] = expTicks.ToString();
                cookie.Values["sig"] = sig;
                cookie.Expires = DateTime.Now.AddDays(REMEMBER_DAYS); // Hạn 7 ngày
                cookie.HttpOnly = true;
                cookie.Path = "/";

                response.Cookies.Remove(REMEMBER_COOKIE_NAME);
                response.Cookies.Add(cookie);
            }
            else
            {
                // Nếu người dùng bỏ chọn ghi nhớ thì xóa cookie cũ (nếu có)
                HttpCookie expiredCookie = new HttpCookie(REMEMBER_COOKIE_NAME)
                {
                    Expires = DateTime.Now.AddDays(-7),
                    Path = "/"
                };
                response.Cookies.Add(expiredCookie);
            }
        }
    }

    /// <summary>
    /// Xóa Session và xóa Cookie ghi nhớ đăng nhập khi Đăng xuất
    /// </summary>
    public static void ClearLoginSessionAndCookie(HttpResponseBase response, HttpSessionStateBase session)
    {
        if (session != null)
        {
            session.Clear();
            session.Remove("UserID");
            session.Remove("UserName");
            session.Remove("SDT");
            session.Remove("Avatar");
            session.Remove("PhanQuyen");
        }

        if (response != null)
        {
            HttpCookie expiredCookie = new HttpCookie(REMEMBER_COOKIE_NAME)
            {
                Expires = DateTime.Now.AddDays(-7),
                HttpOnly = true,
                Path = "/"
            };
            response.Cookies.Remove(REMEMBER_COOKIE_NAME);
            response.Cookies.Add(expiredCookie);
        }
    }

    /// <summary>
    /// Tự động khôi phục phiên đăng nhập từ Cookie 7 ngày nếu Session bị mất (do đóng trình duyệt hoặc khởi động lại Server)
    /// </summary>
    public static bool TryRestoreSessionFromCookie(HttpContext context)
    {
        try
        {
            if (context == null || context.Session == null || context.Request == null)
                return false;

            if (context.Session["UserID"] != null)
                return true;

            HttpCookie cookie = context.Request.Cookies[REMEMBER_COOKIE_NAME];
            if (cookie == null)
                return false;

            string uidStr = cookie.Values["uid"];
            string expStr = cookie.Values["exp"];
            string sig = cookie.Values["sig"];

            if (string.IsNullOrEmpty(uidStr) || string.IsNullOrEmpty(expStr) || string.IsNullOrEmpty(sig))
                return false;

            if (!int.TryParse(uidStr, out int userId) || !long.TryParse(expStr, out long expTicks))
                return false;

            // Kiểm tra hạn 7 ngày
            if (DateTime.UtcNow.Ticks > expTicks)
                return false;

            using (var db = new DaTa_Phong_TroEntities())
            {
                var account = db.Tai_Khoan.FirstOrDefault(tk => tk.ID_Tk == userId);
                if (account == null || account.Trang_Thai == false)
                    return false;

                string expectedSig = HashPassword($"{account.ID_Tk}|{account.Pass ?? ""}|{expTicks}|{COOKIE_SECRET}");
                if (!string.Equals(sig, expectedSig, StringComparison.OrdinalIgnoreCase))
                    return false;

                context.Session.Timeout = REMEMBER_DAYS * 24 * 60;
                context.Session["UserID"] = account.ID_Tk;
                context.Session["UserName"] = account.Name;
                context.Session["SDT"] = account.SDT;
                context.Session["Avatar"] = account.Avata;
                context.Session["PhanQuyen"] = account.ID_Phan_Quyen;
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
