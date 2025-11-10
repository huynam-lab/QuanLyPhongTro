using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;

public static class Utilities
{
    public static string HashPassword(string password)
    {
        // Kiểm tra nếu mật khẩu là null hoặc rỗng, trả về null hoặc chuỗi rỗng
        if (string.IsNullOrEmpty(password))
        {
            return null;
            // Lựa chọn trả về null: Dễ dàng kiểm tra trong Controller.
            // Hoặc return string.Empty; nếu bạn thích chuỗi rỗng hơn.
        }

        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            // Chắc chắn rằng password đã có giá trị tại đây
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}