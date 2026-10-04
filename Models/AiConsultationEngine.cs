using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLyPhongTro.Models
{
    public enum UserChatIntent
    {
        GreetingOrConsultRequest,     // "tôi cần tư vấn ạ", "chào bạn", "alo", "tìm phòng giúp mình" (chưa có tiêu chí)
        FaqDeposit,                   // Hỏi về tiền đặt cọc, quy tắc cọc, mất cọc, trả cọc
        FaqElectricityWater,          // Hỏi về giá điện, giá nước, công tơ, wifi, phí dịch vụ
        FaqContractAndLegal,          // Hỏi về hợp đồng thuê nhà, đăng ký tạm trú, bồi thường, thanh lý
        FaqScamWarning,               // Cảnh báo bẫy lừa đảo, cọc ảo, phòng giá rẻ bất thường
        FaqRoomViewingChecklist,      // Kinh nghiệm khi đi xem phòng thực tế (nước, ẩm mốc, an ninh, giờ giấc)
        FaqMarketPriceOverview,       // Hỏi giá trung bình các quận tại Hà Nội, khu nào rẻ/đắt
        WebsiteGuidance,              // Hỏi về chức năng website: đăng ký, đăng nhập, đăng tin, tìm kiếm, yêu thích...
        CheckApiStatus,               // Kiểm tra tình trạng API Key, token, quota
        RoomComparison,               // So sánh giữa các phòng trọ
        SpecificRoomInquiry,          // Hỏi sâu về 1 phòng trọ cụ thể (ưu điểm, có gì tốt, có nên thuê...)
        RoomSearch,                   // Tìm kiếm phòng cụ thể (đã có khu vực, mức giá hoặc tiện ích)
        FreeChat                      // Câu hỏi ngoài chủ đề (spam, tán gẫu, câu hỏi linh tinh) — bot khéo từ chối và dẫn về chủ đề
    }

    public class ExtractedSearchCriteria
    {
        public string RawDistrict { get; set; }
        public string NormalizedDistrict { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public bool HasVideo { get; set; }
        public bool HasGacXep { get; set; }
        public bool HasBanCong { get; set; }
        public bool HasFullNoiThat { get; set; }
        public bool HasKhepKin { get; set; }

        public bool HasAnyCriteria => !string.IsNullOrEmpty(RawDistrict) ||
                                      MinPrice.HasValue ||
                                      MaxPrice.HasValue ||
                                      TargetPrice.HasValue ||
                                      HasVideo ||
                                      HasGacXep ||
                                      HasBanCong ||
                                      HasFullNoiThat ||
                                      HasKhepKin;
    }

    public class ConsultationAdviceResult
    {
        public UserChatIntent Intent { get; set; }
        public string ReplyText { get; set; }
        public List<string> SuggestedQuestions { get; set; } = new List<string>();
        public ExtractedSearchCriteria Criteria { get; set; } = new ExtractedSearchCriteria();
    }

    /// <summary>
    /// Bộ não tư vấn chuyên sâu BĐS Thuê Trọ: Phân loại ý định, trích xuất tiêu chí và cung cấp tri thức chuyên gia
    /// </summary>
    public static class AiConsultationEngine
    {
        private static readonly string[] KnownDistricts = new[]
        {
            "Cầu Giấy", "Hoàn Kiếm", "Đống Đa", "Hai Bà Trưng", "Thanh Xuân",
            "Ba Đình", "Tây Hồ", "Nam Từ Liêm", "Bắc Từ Liêm", "Hoàng Mai",
            "Hà Đông", "Long Biên", "Gia Lâm", "Thanh Trì"
        };

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }

        /// <summary>
        /// Trích xuất tiêu chí tìm kiếm từ câu hỏi của người dùng (và lịch sử trước đó nếu có)
        /// </summary>
        public static ExtractedSearchCriteria ExtractCriteria(string question)
        {
            var criteria = new ExtractedSearchCriteria();
            if (string.IsNullOrWhiteSpace(question)) return criteria;

            string lower = RemoveDiacritics(question.ToLower());

            // 1. Nhận diện quận/khu vực - Trước tiên khớp danh sách chuẩn
            foreach (var dist in KnownDistricts)
            {
                string normDist = RemoveDiacritics(dist.ToLower());
                if (lower.Contains(normDist))
                {
                    criteria.RawDistrict = dist;
                    criteria.NormalizedDistrict = normDist;
                    break;
                }
            }

            // 1b. Nhận diện địa danh linh hoạt (phường/xã/thị trấn/thôn/khu) không có trong danh sách cứng
            if (string.IsNullOrEmpty(criteria.RawDistrict))
            {
                // Khớp "phường X", "xã X", "quận X", "huyện X", "khu X", "thị trấn X"
                var locationMatch = Regex.Match(question,
                    @"\b(phường|xã|quận|huyện|khu|thị\s*trấn|thôn|phố|làng|tổ\s*dân\s*phố)\s+([\w\s]{2,25}?)\b",
                    RegexOptions.IgnoreCase);
                if (locationMatch.Success)
                {
                    string locType = locationMatch.Groups[1].Value.Trim();
                    string locName = locationMatch.Groups[2].Value.Trim();
                    // Loại bỏ các từ thừa ở cuối (không, có, nào, ạ, không ạ...)
                    locName = Regex.Replace(locName, @"\s*(không|có|nào|ạ|nhé|à|nhỉ|đó|này|đây|gần|cạnh)\s*$", "", RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrWhiteSpace(locName) && locName.Length >= 2)
                    {
                        criteria.RawDistrict = locType + " " + locName;
                        criteria.NormalizedDistrict = RemoveDiacritics((locType + " " + locName).ToLower());
                    }
                }
            }

            // 2. Nhận diện mức giá thông minh
            // 2.1. Khoảng giá: "từ 2 đến 4 triệu", "2-4tr", "từ 8 đến 12 triệu", "khoảng 7 tới 10tr"
            var rangeMatch = Regex.Match(lower, @"(tu\s*)?([0-9]+([.,][0-9]+)?)\s*(trieu|tr|m)?\s*(den|-|toi)\s*([0-9]+([.,][0-9]+)?)\s*(trieu|tr|m)");
            if (rangeMatch.Success)
            {
                string sMin = rangeMatch.Groups[2].Value.Replace(',', '.');
                string sMax = rangeMatch.Groups[6].Value.Replace(',', '.');
                if (decimal.TryParse(sMin, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal minVal) &&
                    decimal.TryParse(sMax, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal maxVal))
                {
                    criteria.MinPrice = minVal * 1000000;
                    criteria.MaxPrice = maxVal * 1000000;
                    criteria.TargetPrice = (criteria.MinPrice + criteria.MaxPrice) / 2;
                }
            }
            else
            {
                // 2.2. Dưới / Tối đa: "dưới 10tr", "< 10 triệu", "tối đa 10tr", "không quá 10 triệu"
                var underMatch = Regex.Match(lower, @"\b(duoi|nho hon|<|toi da|cao nhat|khong qua)\s*([0-9]+([.,][0-9]+)?)\s*(trieu|tr|m)?\b");
                if (underMatch.Success)
                {
                    string sVal = underMatch.Groups[2].Value.Replace(',', '.');
                    if (decimal.TryParse(sVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pVal))
                    {
                        criteria.MaxPrice = pVal * 1000000;
                        criteria.TargetPrice = criteria.MaxPrice;
                    }
                }
                else
                {
                    // 2.3. Trên / Tối thiểu: "trên 10tr", "> 10 triệu", "từ 10 triệu trở lên", "tối thiểu 10tr"
                    var overMatch = Regex.Match(lower, @"\b(tren|lon hon|>|toi thieu|thap nhat)\s*([0-9]+([.,][0-9]+)?)\s*(trieu|tr|m)?\b");
                    if (overMatch.Success)
                    {
                        string sVal = overMatch.Groups[2].Value.Replace(',', '.');
                        if (decimal.TryParse(sVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pVal))
                        {
                            criteria.MinPrice = pVal * 1000000;
                            criteria.TargetPrice = criteria.MinPrice;
                        }
                    }
                    else
                    {
                        // 2.4. Giá đơn lập / Tầm / Khoảng: "10tr có phòng nào", "tầm 10 triệu", "khoảng 10tr", "tài chính 10tr", "10 triệu"
                        var singlePriceMatch = Regex.Match(lower, @"\b(tam|khoang|xung quanh|tai chinh|ngan sach|tam gia)?\s*([0-9]+([.,][0-9]+)?)\s*(trieu|tr|m)\b");
                        if (singlePriceMatch.Success)
                        {
                            string sVal = singlePriceMatch.Groups[2].Value.Replace(',', '.');
                            if (decimal.TryParse(sVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal sPrice))
                            {
                                decimal target = sPrice * 1000000;
                                criteria.TargetPrice = target;

                                // Xác định dải giá phù hợp quanh mức mong muốn của khách
                                if (target >= 5000000)
                                {
                                    criteria.MinPrice = target * 0.70m;
                                    criteria.MaxPrice = target * 1.25m;
                                }
                                else
                                {
                                    criteria.MinPrice = Math.Max(1000000, target * 0.60m);
                                    criteria.MaxPrice = target * 1.25m;
                                }
                            }
                        }
                        else if (lower.Contains("gia re") || lower.Contains("re nhat") || lower.Contains("re") || lower.Contains("sinh vien"))
                        {
                            criteria.MaxPrice = 3500000;
                            criteria.TargetPrice = 2700000;
                        }
                    }
                }
            }

            // 3. Nhận diện tiện ích
            criteria.HasVideo = lower.Contains("video") || lower.Contains("clip") || lower.Contains("xem truoc");
            criteria.HasGacXep = lower.Contains("gac") || lower.Contains("gac xep") || lower.Contains("gac lung");
            criteria.HasBanCong = lower.Contains("ban cong") || lower.Contains("thoang mat") || lower.Contains("cua so");
            criteria.HasFullNoiThat = lower.Contains("noi that") || lower.Contains("full do") || lower.Contains("day du do");
            criteria.HasKhepKin = lower.Contains("khep kin") || lower.Contains("ve sinh rieng") || lower.Contains("wc rieng");

            return criteria;
        }

        /// <summary>
        /// Phân loại ý định chính của người dùng dựa trên ngôn ngữ tự nhiên
        /// </summary>
        public static UserChatIntent ClassifyIntent(string question, ExtractedSearchCriteria criteria)
        {
            string lower = RemoveDiacritics(question.ToLower().Trim());

            // 1. Kiểm tra chức năng website (ưu tiên cao — hỏi về cách dùng web)
            if (lower.Contains("dang ky") || lower.Contains("tao tai khoan") || lower.Contains("mo tai khoan") ||
                lower.Contains("dang nhap") || lower.Contains("quen mat khau") || lower.Contains("dang xuat") ||
                lower.Contains("dang tin") || lower.Contains("dang bai") || lower.Contains("dang phong") ||
                lower.Contains("them vao yeu thich") || lower.Contains("luu tin") || lower.Contains("tin da luu") ||
                lower.Contains("tim kiem") && (lower.Contains("tren web") || lower.Contains("tren trang") || lower.Contains("trang web")) ||
                lower.Contains("chuc nang") || lower.Contains("huong dan su dung") || lower.Contains("huong dan cach") ||
                lower.Contains("can ho chung cu") && lower.Contains("trang") ||
                lower.Contains("bo loc") && lower.Contains("web") ||
                lower.Contains("website") || lower.Contains("trang web") ||
                lower.Contains("xem phong chi tiet") ||
                (lower.Contains("huong dan") && (lower.Contains("dang") || lower.Contains("tai khoan") || lower.Contains("web") || lower.Contains("trang"))))
            {
                return UserChatIntent.WebsiteGuidance;
            }

            // 2. Cảnh giác bẫy lừa đảo
            if (lower.Contains("lua dao") || lower.Contains("bay lua") || lower.Contains("co mat tien khong") || lower.Contains("canh giac"))
            {
                return UserChatIntent.FaqScamWarning;
            }

            // 3. Hỏi kinh nghiệm xem phòng / checklist
            if (lower.Contains("xem phong") || lower.Contains("kinh nghiem") || lower.Contains("checklist") || lower.Contains("luu y khi di thue") || lower.Contains("meo di thue"))
            {
                return UserChatIntent.FaqRoomViewingChecklist;
            }

            // 4. Hỏi về giá điện nước, chi phí sinh hoạt
            if (lower.Contains("dien nuoc") || lower.Contains("tien dien") || lower.Contains("tien nuoc") || lower.Contains("gia dien") || lower.Contains("gia nuoc") || lower.Contains("cong to"))
            {
                return UserChatIntent.FaqElectricityWater;
            }

            // 5. Hỏi về tiền đặt cọc
            if (lower.Contains("dat coc") || lower.Contains("tien coc") || lower.Contains("coc tien") || lower.Contains("mat coc") || lower.Contains("tra coc") || lower.Contains("hoan coc"))
            {
                return UserChatIntent.FaqDeposit;
            }

            // 6. Hỏi về hợp đồng thuê nhà, tạm trú, pháp lý
            if (lower.Contains("hop dong") || lower.Contains("tam tru") || lower.Contains("giay to") || lower.Contains("phap ly") || lower.Contains("chu nha") || lower.Contains("thanh ly"))
            {
                return UserChatIntent.FaqContractAndLegal;
            }

            // 7. Hỏi mặt bằng giá thị trường tại Hà Nội
            if (lower.Contains("khu nao re") || lower.Contains("gia trung binh") || lower.Contains("nen thue o dau") || lower.Contains("quan nao"))
            {
                return UserChatIntent.FaqMarketPriceOverview;
            }

            // 8. Kiểm tra trạng thái API / Token
            if (lower.Contains("kiem tra api") || lower.Contains("check api") || lower.Contains("test api") || lower.Contains("trang thai api") || lower.Contains("token con khong") || lower.Contains("api key con khong"))
            {
                return UserChatIntent.CheckApiStatus;
            }

            // 8b. So sánh giữa các phòng trọ
            if (lower.Contains("so voi") || lower.Contains("so sanh") || lower.Contains("khac gi") ||
                lower.Contains("tot hon can nao") || lower.Contains("nen chon can nao") || lower.Contains("nen thue can nao") ||
                lower.Contains("khac nhau the nao") || lower.Contains("soh sanh"))
            {
                return UserChatIntent.RoomComparison;
            }

            // 8c. Hỏi sâu / tìm hiểu chi tiết ưu thế của 1 phòng cụ thể
            if (lower.Contains("co gi tot") || lower.Contains("uu diem") || lower.Contains("diem manh") ||
                lower.Contains("co gi hay") || lower.Contains("co nen thue") || lower.Contains("phong nay the nao") ||
                lower.Contains("phong nay co tot khong") || lower.Contains("co dang tien khong") || lower.Contains("co nhung tien ich gi"))
            {
                return UserChatIntent.SpecificRoomInquiry;
            }

            // 9. Tìm kiếm phòng: có tiêu chí cụ thể (khu vực, giá, tiện ích) HOẶC câu hỏi có từ khóa tìm phòng rõ ràng
            if (!string.IsNullOrEmpty(criteria.RawDistrict) || criteria.MinPrice.HasValue || criteria.MaxPrice.HasValue ||
                criteria.TargetPrice.HasValue || criteria.HasGacXep || criteria.HasVideo || criteria.HasFullNoiThat ||
                criteria.HasBanCong || criteria.HasKhepKin)
            {
                return UserChatIntent.RoomSearch;
            }

            // 10. Heuristic: câu có từ "phòng" + địa danh hay đặc điểm → tìm phòng
            bool hasFindKeyword = lower.Contains("tim phong") || lower.Contains("co phong") ||
                                  lower.Contains("phong nao") || lower.Contains("phong tro") ||
                                  lower.Contains("cho thue") || lower.Contains("nha tro") ||
                                  lower.Contains("studio") || lower.Contains("can ho");
            bool hasLocationHint = lower.Contains("phuong") || lower.Contains("xa ") || lower.Contains("khu ") ||
                                   lower.Contains("quan ") || lower.Contains("huyen ") || lower.Contains("gan ") ||
                                   lower.Contains("thi tran") || lower.Contains("duong ") || lower.Contains("ngo ");
            if (hasFindKeyword || (hasLocationHint && (lower.Contains("phong") || lower.Contains("nha"))))
            {
                return UserChatIntent.RoomSearch;
            }

            // 11. Phát hiện câu hỏi hoàn toàn ngoài chủ đề / spam / tán gẫu vô nghĩa
            // Nếu câu hỏi quá ngắn (1-2 ký tự), hoặc chứa từ khóa rõ ràng ngoài lĩnh vực
            bool isSpam = lower.Length <= 3 && !lower.Contains("alo") && !lower.Contains("hi");
            bool isOffTopic = lower.Contains("thoi tiet") || lower.Contains("bong da") ||
                              lower.Contains("phim") || lower.Contains("game") ||
                              lower.Contains("nau an") || lower.Contains("cong thuc") ||
                              lower.Contains("giai toan") || lower.Contains("thu do") ||
                              lower.Contains("dich thuat") || lower.Contains("kieu") ||
                              lower.Contains("may tinh") || lower.Contains("lap trinh") ||
                              lower.Contains("lich su") || lower.Contains("khoa hoc") ||
                              lower.Contains("am nhac") || lower.Contains("ca si") ||
                              lower.Contains("yeu") || lower.Contains("hen ho") ||
                              lower.Contains("nguoi yeu") || lower.Contains("tinh yeu") ||
                              lower.Contains("choi gi") || lower.Contains("an gi") ||
                              lower.Contains("ban la ai") || lower.Contains("may la gi") ||
                              lower.Contains("chat gpt") || lower.Contains("gemini") ||
                              Regex.IsMatch(lower, @"^[a-z\s]{1,8}$") && !lower.Contains("phong") && !lower.Contains("nha") && !lower.Contains("thue");

            if (isSpam || isOffTopic)
            {
                return UserChatIntent.FreeChat;
            }

            // 12. Mặc định: Chào hỏi, yêu cầu tư vấn chung chưa có tiêu chí cụ thể
            return UserChatIntent.GreetingOrConsultRequest;
        }


        /// <summary>
        /// Bản đồ ánh xạ quận huyện Hà Nội sang các phường/tuyến phố tương ứng trong CSDL
        /// </summary>
        public static List<string> GetMatchingWardKeywords(string district)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(district)) return list;

            list.Add(district.Trim());
            string norm = RemoveDiacritics(district.ToLower());

            // Cầu Giấy -> giáp trực tiếp Phường Láng (ngay gần ĐH Ngoại Thương, ĐH Luật, Ngoại Giao), Giảng Võ, Ba Đình
            if (norm.Contains("cau giay"))
            {
                list.AddRange(new[] { "Láng", "Láng Hạ", "Giảng Võ", "Ba Đình", "Ngọc Hà" });
            }
            // Đống Đa -> Láng, Kim Liên, Ô Chợ Dừa, Văn Miếu, Giảng Võ
            else if (norm.Contains("dong da"))
            {
                list.AddRange(new[] { "Láng", "Kim Liên", "Ô Chợ Dừa", "Văn Miếu", "Giảng Võ" });
            }
            // Ba Đình -> Ngọc Hà, Giảng Võ, Ba Đình, Cửa Nam
            else if (norm.Contains("ba dinh"))
            {
                list.AddRange(new[] { "Ngọc Hà", "Giảng Võ", "Ba Đình", "Cửa Nam" });
            }
            // Hoàn Kiếm -> Cửa Nam, Hồng Hà, Hoàn Kiếm
            else if (norm.Contains("hoan kiem"))
            {
                list.AddRange(new[] { "Cửa Nam", "Hồng Hà", "Hoàn Kiếm" });
            }
            // Hai Bà Trưng / Hoàng Mai -> Hoàng Mai, Lĩnh Nam, Vĩnh Hưng, Kim Liên
            else if (norm.Contains("hai ba trung") || norm.Contains("hoang mai"))
            {
                list.AddRange(new[] { "Hoàng Mai", "Lĩnh Nam", "Vĩnh Hưng", "Kim Liên" });
            }
            // Thanh Xuân -> Láng, Ô Chợ Dừa
            else if (norm.Contains("thanh xuan"))
            {
                list.AddRange(new[] { "Láng", "Ô Chợ Dừa" });
            }

            return list.Distinct().ToList();
        }

        /// <summary>
        /// Tạo câu trả lời tư vấn chuyên sâu cho các tình huống nghiệp vụ thực tế
        /// </summary>
        public static ConsultationAdviceResult GenerateDeepAdvice(UserChatIntent intent, string userQuestion, ExtractedSearchCriteria criteria)
        {
            var result = new ConsultationAdviceResult { Intent = intent, Criteria = criteria };

            switch (intent)
            {
                case UserChatIntent.GreetingOrConsultRequest:
                    result.ReplyText =
                        "Dạ, xin chào bạn! 👋 Mình là **Chuyên Viên Tư Vấn Thuê Trọ AI** của hệ thống.\n\n" +
                        "Mình rất sẵn lòng hỗ trợ bạn tìm được căn phòng ưng ý, giá tốt và an tâm nhất. " +
                        "Để mình lọc phòng chuẩn xác nhất cho bạn, **bạn có thể cho mình biết thêm:**\n\n" +
                        "1. 📍 **Khu vực mong muốn**: Bạn muốn ở khu vực nào (hoặc gần trường ĐH / công ty nào như Cầu Giấy, Hoàn Kiếm, Đống Đa, Thanh Xuân...)?\n" +
                        "2. 💰 **Tầm tài chính hàng tháng**: Ngân sách bạn dự trù khoảng bao nhiêu (ví dụ: dưới 2.5 triệu, 3 - 4.5 triệu)?\n" +
                        "3. 🏠 **Tiêu chí ưu tiên**: Bạn ở 1 mình hay ở ghép, cần có **gác xép**, **ban công**, **thang máy**, hay **nội thất đầy đủ**?\n\n" +
                        "👉 Ngoài ra, bạn cũng có thể hỏi mình về **thủ tục cọc, hợp đồng pháp lý, giá điện nước** hoặc kinh nghiệm tránh bẫy lừa đảo nhé!";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Tìm phòng Cầu Giấy dưới 3.5 triệu",
                        "Phòng ở Hoàn Kiếm có gác xép",
                        "Tiền đặt cọc và giá điện nước tính sao?",
                        "Kinh nghiệm xem phòng tránh bị lừa"
                    };
                    break;

                case UserChatIntent.FaqDeposit:
                    result.ReplyText =
                        "💡 **Cẩm nang chuyên sâu về Tiền Đặt Cọc khi thuê phòng trọ:**\n\n" +
                        "1. **Mức đặt cọc tiêu chuẩn thị trường:**\n" +
                        "   • Với phòng trọ bình dân/sinh viên: Đặt cọc **1 tháng** tiền phòng (hoặc 0.5 tháng nếu thỏa thuận tốt).\n" +
                        "   • Với chung cư mini/căn hộ dịch vụ: Đặt cọc **1 đến 2 tháng** tùy vào giá trị đồ đạc nội thất đi kèm.\n\n" +
                        "2. **Quy tắc vàng để không bị mất cọc:**\n" +
                        "   • **Giấy biên nhận đặt cọc**: Bắt buộc phải có chữ ký hai bên, ghi rõ số tiền cọc, ngày bắt đầu tính tiền phòng, và thời hạn hoàn cọc.\n" +
                        "   • **Điều khoản hoàn cọc**: Hợp đồng phải ghi rõ: *'Bên thuê được hoàn lại 100% tiền cọc khi thông báo trước thời gian chuyển đi (thường từ 15 đến 30 ngày) và thanh toán đủ điện nước'*\n" +
                        "   • **Biên bản hiện trạng đồ đạc**: Chụp ảnh, quay video hiện trạng tường, cửa sổ, thiết bị điện (bình nóng lạnh, điều hòa) ngay lúc nhận phòng để tránh tranh chấp khi dọn đi.\n\n" +
                        "⚠️ *Lưu ý*: Tuyệt đối không chuyển khoản cọc giữ chỗ khi chưa đến tận nơi xem phòng và chưa gặp trực tiếp chủ nhà!";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Lưu ý khi ký hợp đồng thuê trọ",
                        "Giá điện nước nhà trọ quy định thế nào?",
                        "Tìm phòng trọ dưới 3 triệu ở Cầu Giấy"
                    };
                    break;

                case UserChatIntent.FaqElectricityWater:
                    result.ReplyText =
                        "⚡ **Thông tin minh bạch về Chi phí Điện, Nước & Phí Dịch vụ (Hà Nội):**\n\n" +
                        "1. **Tiền điện:**\n" +
                        "   • Giá phổ biến tại các nhà trọ/CCMN tư nhân: **3.500đ - 4.000đ / kWh (số)**.\n" +
                        "   • *Mẹo kiểm tra*: Lúc chuyển vào, hãy chụp lại chỉ số công tơ điện ngay trước mặt chủ nhà. Hãy kiểm tra xem công tơ có quay bất thường khi ngắt toàn bộ aptomat trong phòng hay không (để tránh bị câu trộm điện).\n\n" +
                        "2. **Tiền nước:**\n" +
                        "   • **Theo khối (đồng hồ riêng)**: Khoảng **25.000đ - 35.000đ / m³**.\n" +
                        "   • **Thu khoán theo đầu người**: Thường từ **80.000đ - 100.000đ / người / tháng** (phù hợp nếu dùng máy giặt nhiều).\n\n" +
                        "3. **Các phụ phí thường gặp cần hỏi rõ từ đầu:**\n" +
                        "   • Mạng Internet / Wifi: 50.000đ - 100.000đ / phòng / tháng.\n" +
                        "   • Vệ sinh & rác sinh hoạt: 30.000đ - 50.000đ / phòng / tháng.\n" +
                        "   • Phí máy giặt chung / Thang máy / Giữ xe (nếu có): Hỏi kỹ miễn phí hay tính theo xe/người.";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Thủ tục hợp đồng thuê nhà gồm những gì?",
                        "Cách kiểm tra phòng trọ lúc đi xem thực tế",
                        "Tìm phòng có máy giặt và ban công"
                    };
                    break;

                case UserChatIntent.FaqContractAndLegal:
                    result.ReplyText =
                        "📝 **Cẩm nang Hợp Đồng Thuê Nhà & Thủ tục Pháp Lý an toàn:**\n\n" +
                        "1. **Thông tin các bên:**\n" +
                        "   • Yêu cầu đối chiếu CCCD của bên cho thuê xem có đúng là chủ nhà (hoặc có giấy ủy quyền/hợp đồng quản lý nếu là môi giới/đơn vị thứ cấp).\n\n" +
                        "2. **Điều khoản thời hạn & chấm dứt hợp đồng:**\n" +
                        "   • Hợp đồng thường kéo dài **6 tháng hoặc 1 năm**.\n" +
                        "   • Điều khoản chuyển đi trước hạn: Quy định rõ phải thông báo trước bao nhiêu ngày (thường 30 ngày) để không bị trừ tiền cọc.\n\n" +
                        "3. **Bảo trì & Sửa chữa:**\n" +
                        "   • Hư hỏng do thời gian hoặc hao mòn tự nhiên (hỏng bình nóng lạnh cũ, rò rỉ ống nước ngầm...) do **Chủ nhà** chịu chi phí.\n" +
                        "   • Hư hỏng do người thuê sơ suất (làm vỡ gương, gãy tay nắm cửa...) do người thuê sửa chữa.\n\n" +
                        "4. **Đăng ký tạm trú:**\n" +
                        "   • Chủ nhà có trách nhiệm phối hợp khai báo tạm trú (CT01/Dịch vụ công). Bạn chỉ cần cung cấp bản chụp CCCD 2 mặt.";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Cảnh giác các chiêu trò lừa đảo cọc",
                        "Checklist khi đi xem phòng thực tế",
                        "Gợi ý phòng trọ giá tốt"
                    };
                    break;

                case UserChatIntent.FaqScamWarning:
                    result.ReplyText =
                        "🛡️ **Cảnh báo 5 Bẫy Lừa Đảo Thuê Trọ phổ biến bạn cần cảnh giác:**\n\n" +
                        "1. **Bẫy phòng ảo giá rẻ**: Đăng ảnh phòng studio cao cấp, full đồ ở trung tâm nhưng giá chỉ 1.5 - 2 triệu/tháng. Khi nhắn tin sẽ giục: *'Đang có nhiều người hỏi, em chuyển cọc giữ chân 500k - 1 triệu kẻo mất'*. Sau khi chuyển khoản xong là khóa nick ngay lập tức!\n" +
                        "2. **Bẫy phí dẫn đường/phí xem phòng**: Cò mồi đòi trả 100k - 200k tiền công dẫn đi xem phòng. Tuyệt đối KHÔNG trả bất kỳ khoản phí nào chỉ để được xem phòng!\n" +
                        "3. **Chủ trọ giả mạo**: Người thuê trọ cũ chuẩn bị dọn đi mạo danh chủ nhà để nhận tiền cọc của khách mới rồi ôm tiền biến mất.\n" +
                        "4. **Đội giá dịch vụ sau khi vào ở**: Tiền phòng thì rẻ nhưng tiền điện 5.000đ/số, nước 150k/người, tiền rác 100k, phụ thu xe máy 200k/tháng. Vì vậy mọi chi phí phải ghi rõ bằng số vào hợp đồng.\n\n" +
                        "✅ **Nguyên tắc vàng**: Chỉ cọc sau khi đã gặp trực tiếp, xem phòng tận mắt và có hợp đồng/biên nhận rõ ràng!";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Kinh nghiệm kiểm tra phòng trọ",
                        "Quy định về tiền đặt cọc",
                        "Tìm phòng trọ chính chủ có video"
                    };
                    break;

                case UserChatIntent.FaqRoomViewingChecklist:
                    result.ReplyText =
                        "🔍 **Checklist 'Vàng' khi đi xem phòng trọ thực tế:**\n\n" +
                        "• 🚰 **Áp lực nước & Hệ thống thoát nước**: Mở vòi hoa sen và xả bồn cầu thử cùng lúc. Quan sát xem sàn nhà tắm có bị đọng nước lâu không, mùi hôi cống có xốc lên không.\n" +
                        "• 📶 **Sóng điện thoại & Ánh sáng**: Kiểm tra xem điện thoại có sóng tốt và 4G ổn định trong phòng không (nhiều phòng trọ tầng thấp/sâu trong ngõ mất hẳn sóng). Mở cửa sổ kiểm tra độ thoáng gió và ánh sáng tự nhiên.\n" +
                        "• 🧱 **Tường & Ẩm mốc**: Nhìn kỹ chân tường và các góc trần xem có vết nứt hoặc vết sơn loang lổ do ngấm nước mưa không.\n" +
                        "• 🚪 **An ninh & Khóa cửa**: Kiểm tra cổng ra vào (khóa vân tay hay thẻ từ), ngõ đi có camera an ninh không, ngõ có bị tối hoặc ngập khi trời mưa to không.\n" +
                        "• 🛵 **Chỗ để xe máy**: Xem khu vực để xe có rộng rãi, dắt xe ra vào giờ cao điểm có dễ dàng không.";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Quy định tiền cọc và hợp đồng",
                        "Chi phí điện nước tính thế nào?",
                        "Tìm phòng trọ ở Cầu Giấy"
                    };
                    break;

                case UserChatIntent.FaqMarketPriceOverview:
                    result.ReplyText =
                        "📊 **Bức tranh mặt bằng giá thuê phòng trọ tại Hà Nội:**\n\n" +
                        "• **Khu vực Cầu Giấy, Đống Đa, Thanh Xuân** (Gần nhiều trường ĐH lớn):\n" +
                        "   - Phòng trọ sinh viên, vệ sinh chung: **1.8 - 2.5 triệu/tháng**.\n" +
                        "   - Phòng khép kín, có gác xép: **2.8 - 3.8 triệu/tháng**.\n" +
                        "   - Căn hộ mini/Studio full nội thất: **4.2 - 6 triệu/tháng**.\n\n" +
                        "• **Khu vực Hoàn Kiếm, Ba Đình, Hai Bà Trưng** (Trung tâm phố cổ):\n" +
                        "   - Mặt bằng giá cao hơn 15-30%, phòng diện tích vừa phải nhưng tiện đi lại.\n\n" +
                        "• **Khu vực Nam/Bắc Từ Liêm, Hà Đông, Hoàng Mai**:\n" +
                        "   - Phòng rất rộng rãi, thoáng mát, giá rẻ hơn từ 20-30% so với khu trung tâm.\n\n" +
                        "👉 Bạn dự định thuê ở quận nào để mình tìm phòng sát nhất theo ngân sách?";
                    result.SuggestedQuestions = new List<string>
                    {
                        "Tìm phòng Cầu Giấy tầm 3 triệu",
                        "Tìm phòng trọ giá rẻ ở Đống Đa",
                        "Phòng có gác xép ở Thanh Xuân"
                    };
                    break;

                case UserChatIntent.WebsiteGuidance:
                    // Câu trả lời fallback (khi LLM không hoạt động)
                    // Thông thường sẽ được LLM xử lý với system prompt đầy đủ
                    string qLower = RemoveDiacritics(userQuestion.ToLower());
                    if (qLower.Contains("dang ky") || qLower.Contains("tao tai khoan") || qLower.Contains("mo tai khoan"))
                    {
                        result.ReplyText =
                            "📋 **Hướng dẫn Đăng ký tài khoản trên website:**\n\n" +
                            "1. Truy cập website và nhấn nút **'Đăng ký'** ở góc trên bên phải màn hình.\n" +
                            "2. Điền đầy đủ thông tin: Họ tên, Số điện thoại, Mật khẩu.\n" +
                            "3. Chọn loại tài khoản:\n" +
                            "   - **Người thuê**: Để tìm kiếm và lưu phòng yêu thích.\n" +
                            "   - **Chủ nhà/Người đăng tin**: Để đăng tin cho thuê phòng.\n" +
                            "4. Nhấn **'Đăng ký'** để hoàn tất. Bạn có thể đăng nhập ngay sau đó.\n\n" +
                            "💡 *Mẹo*: Số điện thoại dùng để đăng nhập nên nhớ chính xác nhé!";
                        result.SuggestedQuestions = new List<string> { "Cách đăng nhập vào tài khoản", "Cách đăng tin phòng cho thuê", "Tìm phòng trọ tốt ở Hà Nội" };
                    }
                    else if (qLower.Contains("dang nhap") || qLower.Contains("quen mat khau"))
                    {
                        result.ReplyText =
                            "🔑 **Hướng dẫn Đăng nhập tài khoản:**\n\n" +
                            "1. Nhấn nút **'Đăng nhập'** ở góc trên bên phải trang web.\n" +
                            "2. Nhập **Số điện thoại** và **Mật khẩu** đã đăng ký.\n" +
                            "3. Nhấn **'Đăng nhập'** để vào tài khoản.\n\n" +
                            "⚠️ *Quên mật khẩu?* Hiện tại vui lòng liên hệ quản trị viên qua số hotline hoặc đăng ký tài khoản mới với số điện thoại khác.";
                        result.SuggestedQuestions = new List<string> { "Cách đăng ký tài khoản mới", "Cách đăng tin cho thuê phòng", "Tìm phòng trọ" };
                    }
                    else if (qLower.Contains("dang tin") || qLower.Contains("dang phong") || qLower.Contains("dang bai"))
                    {
                        result.ReplyText =
                            "📝 **Hướng dẫn Đăng tin cho thuê phòng trọ:**\n\n" +
                            "1. **Đăng nhập** vào tài khoản Chủ nhà/Người đăng tin.\n" +
                            "2. Vào **Trang quản lý** → Chọn **'Đăng tin mới'**.\n" +
                            "3. Điền đầy đủ thông tin:\n" +
                            "   - Tiêu đề tin, Mô tả chi tiết phòng\n" +
                            "   - Giá thuê, Diện tích\n" +
                            "   - Địa chỉ và Khu vực\n" +
                            "   - Upload ảnh phòng (khuyến khích 3-5 ảnh chất lượng cao)\n" +
                            "4. Chọn **Loại tin** (Tin thường / Tin VIP) và nhấn **Đăng tin**.\n\n" +
                            "💡 *Mẹo*: Tin có ảnh rõ nét và mô tả chi tiết sẽ nhận được nhiều lượt xem hơn!";
                        result.SuggestedQuestions = new List<string> { "Cách đăng ký tài khoản chủ nhà", "Giá các gói đăng tin", "Tìm phòng trọ" };
                    }
                    else if (qLower.Contains("yeu thich") || qLower.Contains("luu tin") || qLower.Contains("tin da luu"))
                    {
                        result.ReplyText =
                            "❤️ **Hướng dẫn lưu Tin yêu thích:**\n\n" +
                            "1. Khi xem danh sách phòng trọ, nhấn vào **biểu tượng ❤️** trên mỗi tin để lưu vào danh sách yêu thích.\n" +
                            "2. Để xem lại các tin đã lưu, vào **Menu → 'Tin đã lưu'** (hoặc 'Yêu thích').\n" +
                            "3. Chức năng này yêu cầu **đăng nhập** tài khoản trước.\n\n" +
                            "💡 Bạn có thể lưu nhiều phòng và so sánh sau để chọn phòng phù hợp nhất!";
                        result.SuggestedQuestions = new List<string> { "Cách đăng nhập", "Tìm phòng Cầu Giấy", "Phòng có gác xép" };
                    }
                    else
                    {
                        result.ReplyText =
                            "🌐 **Hướng dẫn sử dụng Website QuanLyPhongTro:**\n\n" +
                            "Website cung cấp đầy đủ các tính năng để bạn tìm thuê phòng hoặc đăng tin cho thuê:\n\n" +
                            "**🔍 Dành cho người tìm phòng:**\n" +
                            "• **Trang chủ**: Xem danh sách phòng trọ mới nhất, lọc theo giá và diện tích\n" +
                            "• **Tìm kiếm**: Lọc theo khu vực (quận/phường), khoảng giá, diện tích\n" +
                            "• **Căn hộ chung cư** / **Căn hộ mini**: Xem riêng từng loại bất động sản\n" +
                            "• **Yêu thích**: Lưu các phòng quan tâm để so sánh sau\n" +
                            "• **Blog & Hỏi đáp**: Kinh nghiệm thuê trọ, mẫu hợp đồng chuẩn\n\n" +
                            "**🏠 Dành cho chủ nhà:**\n" +
                            "• **Đăng ký** tài khoản chủ nhà → **Đăng tin** phòng cho thuê\n" +
                            "• Upload ảnh, video phòng để tăng lượt xem\n\n" +
                            "Bạn cần hướng dẫn chi tiết về tính năng nào?";
                        result.SuggestedQuestions = new List<string> { "Cách đăng ký tài khoản", "Cách đăng tin cho thuê phòng", "Tìm phòng trọ ở Hà Nội" };
                    }
                    break;

                case UserChatIntent.SpecificRoomInquiry:
                    result.ReplyText =
                        "🌟 **TƯ VẤN CHUYÊN SÂU GIÁ TRỊ CỦA CĂN PHÒNG BẠN QUAN TÂM:**\n\n" +
                        "Căn phòng này sở hữu nhiều ưu điểm vượt trội rất đáng để bạn cân nhắc và xuống tiền thuê ngay:\n\n" +
                        "1. 🍃 **Không gian sống & Vi khí hậu**: Phòng thiết kế thông thoáng, đón trọn ánh sáng tự nhiên và gió trời, giúp không gian luôn khô ráo, sạch sẽ, tiết kiệm điện năng ban ngày và rất tốt cho sức khỏe.\n" +
                        "2. 🛋️ **Tối ưu chi phí ban đầu**: Được trang bị tiện nghi thiết yếu sẵn có, bạn chỉ cần xách vali vào ở mà không tốn thêm hàng triệu đồng để sắm sửa nội thất mới.\n" +
                        "3. 🔒 **An ninh & Giờ giấc**: Khu vực dân trí cao, an ninh đảm bảo, giờ giấc sinh hoạt tự do, tạo cảm giác thoải mái và an tâm tuyệt đối sau một ngày làm việc/học tập căng thẳng.\n" +
                        "4. 📍 **Vị trí thuận tiện**: Nằm ở vị trí đắc địa, dễ dàng kết nối đến các trường đại học lớn, văn phòng công sở, chợ dân sinh và các tuyến đường huyết mạch.\n\n" +
                        "👉 **Lời khuyên từ chuyên gia**: Căn phòng có vị trí và tầm giá như thế này thường được khách chốt cọc rất nhanh. Bạn hãy bấm vào **thẻ phòng bên dưới** để xem ảnh, video thực tế và liên hệ trực tiếp chủ nhà để hẹn lịch xem phòng sớm nhé!";
                    result.SuggestedQuestions = new List<string> { "Xem thêm phòng tương tự", "Hỏi thủ tục đặt cọc", "Kinh nghiệm xem phòng thực tế" };
                    break;

                case UserChatIntent.RoomComparison:
                    result.ReplyText =
                        "⚖️ **PHÂN TÍCH & SO SÁNH GIỮA CÁC CĂN PHÒNG BẠN ĐANG PHÂN VÂN:**\n\n" +
                        "Cả hai căn phòng này đều là những lựa chọn sáng giá trên hệ thống, mỗi căn đều sở hữu những thế mạnh riêng biệt:\n\n" +
                        "• **Về Không gian & Trải nghiệm sống**: Căn có ban công/cửa sổ lớn sẽ ưu tiên tối đa cho sự thông thoáng, yên tĩnh và thư giãn sau giờ làm việc; trong khi căn hộ mini khép kín lại tối ưu sự tiện nghi, hiện đại và riêng tư tuyệt đối.\n" +
                        "• **Về Vị trí & Tiện ích kết nối**: Hãy cân nhắc lộ trình di chuyển hàng ngày của bạn — nếu công việc/học tập gần khu vực nào hơn hoặc cần gần các trung tâm thương mại lớn, hãy ưu tiên căn ở khu vực đó để tiết kiệm thời gian di chuyển.\n" +
                        "• **Về Ngân sách & Giá trị nhận lại**: So sánh đơn giá/m² và các tiện nghi đi kèm (máy giặt, điều hòa, gác xép, thang máy) để thấy căn nào mang lại giá trị sinh hoạt tốt nhất cho túi tiền của bạn.\n\n" +
                        "🎯 **Gợi ý lựa chọn**:\n" +
                        "- Chọn căn thứ nhất nếu bạn thích không gian thoáng đãng, yên tĩnh để nghỉ ngơi hoặc làm việc tại nhà.\n" +
                        "- Chọn căn thứ hai nếu bạn ưu tiên sự tiện lợi, vị trí sôi động, gần các khu dịch vụ vui chơi giải trí.\n\n" +
                        "👉 Mời bạn tham khảo chi tiết cả 2 căn phòng bên dưới và liên hệ chủ nhà để chốt lịch xem trực tiếp!";
                    result.SuggestedQuestions = new List<string> { "Xem thêm phòng cùng khu vực", "Cách thương lượng giá phòng", "Thủ tục ký hợp đồng" };
                    break;

                case UserChatIntent.FreeChat:
                    // Kiểm tra an toàn bảo mật: Từ chối các lệnh SQL, đòi quyền admin hoặc dò mật khẩu
                    string qCheck = RemoveDiacritics(userQuestion?.ToLower() ?? "");
                    if (qCheck.Contains("drop table") || qCheck.Contains("select *") || qCheck.Contains("insert into") ||
                        qCheck.Contains("delete from") || qCheck.Contains("update ") || qCheck.Contains("mat khau") ||
                        qCheck.Contains("password") || (qCheck.Contains("admin") && (qCheck.Contains("quyen") || qCheck.Contains("cap quyen") || qCheck.Contains("pass"))))
                    {
                        result.ReplyText = "Haha, tính hack Mia hay gì nè? 😂 Mia chỉ là bot chat trò chuyện với bạn như một người dùng bình thường thôi, không có quyền hạn chạy lệnh SQL, can thiệp hệ thống hay xem mật khẩu của ai đâu nha! Đừng thử thách Mia tội nghiệp 😄";
                        result.SuggestedQuestions = new List<string> { "Tìm phòng trọ ở Cầu Giấy", "Kinh nghiệm xem phòng", "Tiền cọc tính thế nào?" };
                        break;
                    }

                    // Phản hồi thân thiện, cởi mở của Mia khi trò chuyện tự do ở chế độ ngoại tuyến
                    var freeChatReplies = new[]
                    {
                        "Chào bạn! Mia đang ở đây nghe bạn chia sẻ nè 😄 Bạn muốn tâm sự điều gì hay cần Mia hỗ trợ thông tin gì không? Cứ thoải mái nhắn cho Mia nhé!",
                        "Rất vui được trò chuyện với bạn! ✨ Mia là trợ lý AI của QuanLyPhongTro.vn. Mình luôn sẵn sàng lắng nghe, tán gẫu cùng bạn, hoặc nếu bạn có nhu cầu tìm phòng trọ tại Hà Nội thì Mia phục vụ ngay nha!",
                        "Haha, nghe thú vị ghê! 😄 Hôm nay của bạn thế nào rồi? Cần tìm hiểu thông tin gì hay muốn tâm sự chuyện thuê trọ, cuộc sống ở Hà Nội cứ kể cho Mia nghe nhé!",
                        "Mia nghe nè! Bạn đang quan tâm chủ đề gì thế? Hãy chia sẻ thêm với mình nhé, Mia rất sẵn lòng trò chuyện cùng bạn 😊",
                        "Chào bạn nhé! Mia có thể vừa trò chuyện cùng bạn, vừa giúp bạn săn những căn phòng trọ siêu đẹp giá tốt tại Hà Nội. Hôm nay bạn đang có dự định gì thế nào?"
                    };
                    var rnd = new Random();
                    result.ReplyText = freeChatReplies[rnd.Next(freeChatReplies.Length)];
                    result.SuggestedQuestions = new List<string>
                    {
                        "Tìm phòng dưới 3 triệu ở Cầu Giấy",
                        "Phòng có ban công và gác xép",
                        "Tiền cọc và giá điện nước tính sao?",
                        "Kinh nghiệm xem phòng tránh bị lừa"
                    };
                    break;
            }

            return result;
        }
    }
}
