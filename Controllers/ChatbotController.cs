using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace QuanLyPhongTro.Controllers
{
    public class ChatbotController : Controller
    {
        private DaTa_Phong_TroEntities db = new DaTa_Phong_TroEntities();

        public class ChatRequest
        {
            public string Question { get; set; }
            public List<ChatHistoryItem> History { get; set; }
        }

        public class RoomDto
        {
            public int id { get; set; }
            public string name { get; set; }
            public string price { get; set; }
            public string area { get; set; }
            public string address { get; set; }
            public string district { get; set; }
            public string areaName { get; set; }
            public string imageUrl { get; set; }
        }

        [HttpPost]
        public async Task<JsonResult> Ask(ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return Json(new
                {
                    success = false,
                    reply = "Xin chào! Mình là Trợ lý AI Thuê Trọ. Bạn đang cần tìm phòng ở khu vực nào hoặc cần tư vấn điều gì, hãy nhắn cho mình nhé!"
                });
            }

            string question = request.Question.Trim();

            try
            {
                // =======================================================
                // 1. TRÍCH XUẤT TIÊU CHÍ & PHÂN LOẠI Ý ĐỊNH
                // =======================================================
                var criteria = AiConsultationEngine.ExtractCriteria(question);

                // Thừa kế ngữ cảnh còn thiếu từ lịch sử hội thoại gần nhất (Multi-turn Context)
                if (request.History != null && request.History.Count > 0)
                {
                    var pastUserMsg = request.History
                        .Where(h => h.Role == "user" && !string.IsNullOrWhiteSpace(h.Content) && !h.Content.Trim().Equals(question, StringComparison.OrdinalIgnoreCase))
                        .LastOrDefault();

                    if (pastUserMsg != null)
                    {
                        var pastCriteria = AiConsultationEngine.ExtractCriteria(pastUserMsg.Content);
                        if (string.IsNullOrEmpty(criteria.RawDistrict) && !string.IsNullOrEmpty(pastCriteria.RawDistrict))
                        {
                            criteria.RawDistrict = pastCriteria.RawDistrict;
                            criteria.NormalizedDistrict = pastCriteria.NormalizedDistrict;
                        }
                        if (!criteria.MinPrice.HasValue && !criteria.MaxPrice.HasValue && !criteria.TargetPrice.HasValue)
                        {
                            criteria.MinPrice = pastCriteria.MinPrice;
                            criteria.MaxPrice = pastCriteria.MaxPrice;
                            criteria.TargetPrice = pastCriteria.TargetPrice;
                        }
                        if (!criteria.HasGacXep && pastCriteria.HasGacXep) criteria.HasGacXep = true;
                        if (!criteria.HasBanCong && pastCriteria.HasBanCong) criteria.HasBanCong = true;
                        if (!criteria.HasVideo && pastCriteria.HasVideo) criteria.HasVideo = true;
                        if (!criteria.HasFullNoiThat && pastCriteria.HasFullNoiThat) criteria.HasFullNoiThat = true;
                    }
                }

                var intent = AiConsultationEngine.ClassifyIntent(question, criteria);

                // Xử lý kiểm tra tình trạng API Key trực tiếp khi khách gõ "kiểm tra api"
                if (intent == UserChatIntent.CheckApiStatus)
                {
                    var healthList = await AiServiceManager.CheckAllProvidersHealthAsync();
                    var sbStatus = new StringBuilder();
                    sbStatus.Append("📊 **BÁO CÁO KIỂM TRA TRẠNG THÁI API KEY & MÔ HÌNH AI:**\n\n");
                    foreach (var h in healthList)
                    {
                        string icon = h.Status.Contains("Sẵn Sàng") ? "🟢" : (h.Status.Contains("Hết Quota") ? "🔴" : "⚪");
                        sbStatus.AppendFormat("{0} **{1}** ({2}): *{3}*\n   - Chi tiết: {4}\n\n",
                            icon, h.ProviderName, h.ModelName, h.Status, h.DetailMessage);
                    }
                    sbStatus.Append("💡 **Cơ chế hoạt động khi hết Token / Quota:**\n");
                    sbStatus.Append("1. Hệ thống tự động Failover sang nhà cung cấp dự phòng tiếp theo cấu hình trong Web.config.\n");
                    sbStatus.Append("2. Nếu toàn bộ API trực tuyến hết hạn mức, **Cỗ máy AI Ngoại Tuyến (Offline Expert Engine)** sẽ tự động tiếp quản, đảm bảo 100% không bao giờ gián đoạn dịch vụ.\n");
                    sbStatus.Append("3. Để đổi API Key mới: Mở file `Web.config` và cập nhật khóa tại `<appSettings>`.");

                    return Json(new
                    {
                        success = true,
                        reply = sbStatus.ToString(),
                        rooms = new List<RoomDto>(),
                        provider = "Diagnostics System",
                        model = "Health Check Monitor",
                        intent = intent.ToString()
                    });
                }

                // =======================================================
                // 2. PHÁT HIỆN PHÒNG ĐƯỢC NHẮC ĐẾN TRONG CÂU HỎI (ROOM RECOGNITION)
                // =======================================================
                var candidateRoomsForPrompt = new List<AiRoomCatalogItem>();
                List<Phong_Tro> matchedCandidates = new List<Phong_Tro>();

                // Quét trong CSDL xem người dùng có trích dẫn mã ID hoặc tiêu đề của phòng nào không
                var referencedRooms = DetectReferencedRoomsInDatabase(question);

                if (referencedRooms.Count >= 2 || (referencedRooms.Count >= 1 && intent == UserChatIntent.RoomComparison))
                {
                    // Trường hợp người dùng muốn so sánh giữa 2 phòng
                    intent = UserChatIntent.RoomComparison;
                    matchedCandidates = referencedRooms.Take(2).ToList();

                    // Nếu người dùng chỉ nhắc 1 phòng nhưng câu hỏi có ý định so sánh -> tìm thêm 1 phòng tương đương để so sánh
                    if (matchedCandidates.Count == 1)
                    {
                        var firstRoom = matchedCandidates[0];
                        var other = db.Phong_Tro
                                      .Include(p => p.Khu_Vuc)
                                      .Include(p => p.Hinh_Anh)
                                      .Include(p => p.Video)
                                      .Include(p => p.Loai_Tin)
                                      .Where(p => p.Trang_Thai == true && p.ID_Phong_Tro != firstRoom.ID_Phong_Tro)
                                      .OrderBy(p => Math.Abs((p.Gia_Ca ?? 0) - (firstRoom.Gia_Ca ?? 0)))
                                      .FirstOrDefault();
                        if (other != null) matchedCandidates.Add(other);
                    }
                }
                else if (referencedRooms.Count == 1)
                {
                    // Người dùng hỏi cụ thể về 1 phòng duy nhất (hỏi ưu điểm, có gì tốt, hay thông tin phòng này)
                    intent = UserChatIntent.SpecificRoomInquiry;
                    matchedCandidates = referencedRooms;
                }
                else if (intent == UserChatIntent.SpecificRoomInquiry)
                {
                    // Có từ khóa hỏi sâu ("có gì tốt", "ưu điểm") nhưng chưa bắt trúng tên phòng chính xác -> Tìm phòng phù hợp nhất
                    matchedCandidates = FindMatchedRooms(criteria).Take(1).ToList();
                    if (!matchedCandidates.Any())
                    {
                        matchedCandidates = db.Phong_Tro
                                              .Include(p => p.Khu_Vuc)
                                              .Include(p => p.Hinh_Anh)
                                              .Include(p => p.Video)
                                              .Include(p => p.Loai_Tin)
                                              .Where(p => p.Trang_Thai == true)
                                              .OrderByDescending(p => p.Gia_Ca)
                                              .Take(1)
                                              .ToList();
                    }
                }
                else if (criteria.HasVideo)
                {
                    // Khách hỏi về video thực tế -> Lấy các phòng có video thực tế trong hệ thống
                    matchedCandidates = db.Phong_Tro
                                          .Include(p => p.Khu_Vuc)
                                          .Include(p => p.Hinh_Anh)
                                          .Include(p => p.Video)
                                          .Include(p => p.Loai_Tin)
                                          .Where(p => p.Trang_Thai == true && p.Video.Any())
                                          .Take(3)
                                          .ToList();
                    intent = UserChatIntent.RoomSearch;
                }
                else if (intent == UserChatIntent.RoomSearch)
                {
                    matchedCandidates = FindMatchedRooms(criteria);

                    // Nếu không có phòng nào thỏa mãn chính xác tiêu chí, lấy các phòng có mức giá sát nhất với mục tiêu của khách
                    if (!matchedCandidates.Any())
                    {
                        decimal fallbackTarget = criteria.TargetPrice ?? criteria.MaxPrice ?? 3000000m;
                        matchedCandidates = db.Phong_Tro
                                              .Include(p => p.Khu_Vuc)
                                              .Include(p => p.Hinh_Anh)
                                              .Include(p => p.Video)
                                              .Include(p => p.Loai_Tin)
                                              .Where(p => p.Trang_Thai == true)
                                              .AsEnumerable()
                                              .OrderBy(p => Math.Abs((p.Gia_Ca ?? 0) - fallbackTarget))
                                              .Take(4)
                                              .ToList();
                    }
                }

                // Đóng gói danh sách ứng viên gửi sang cho LLM phân tích và tư vấn
                if (intent == UserChatIntent.SpecificRoomInquiry || intent == UserChatIntent.RoomComparison || intent == UserChatIntent.RoomSearch)
                {
                    int takeLimit = (intent == UserChatIntent.SpecificRoomInquiry) ? 1 : ((intent == UserChatIntent.RoomComparison) ? 2 : 4);
                    candidateRoomsForPrompt = matchedCandidates.Take(takeLimit).Select(p => new AiRoomCatalogItem
                    {
                        Id = p.ID_Phong_Tro,
                        Title = p.Ten_Phong,
                        Price = p.Gia_Ca ?? 0,
                        Area = p.Dien_Tich ?? 0,
                        District = p.Khu_Vuc != null ? p.Khu_Vuc.Ten_KV : "",
                        Address = p.Dia_Chi ?? "",
                        HasVideo = p.Video != null && p.Video.Any(),
                        KeyAmenities = BuildKeyAmenities(p),
                        Description = !string.IsNullOrWhiteSpace(p.Mo_Ta)
                            ? (p.Mo_Ta.Length > 280 ? p.Mo_Ta.Substring(0, 280) + "..." : p.Mo_Ta)
                            : ""
                    }).ToList();
                }

                // =======================================================
                // 3. THỬ GỌI MULTI-PROVIDER LLM (GROQ, GEMINI, OPENAI...)
                // Áp dụng với MỌI loại intent — LLM luôn được thử trước, Offline là fallback
                // =======================================================
                var providers = AiServiceManager.GetConfiguredProviders();
                var failedCalls = new List<ProviderCallResult>();

                foreach (var provider in providers)
                {
                    if (provider.IsConfigured())
                    {
                        try
                        {
                            var callResult = await provider.AskWithDiagnosticsAsync(question, candidateRoomsForPrompt, request.History);
                            if (callResult != null && callResult.Success && callResult.Response != null && !string.IsNullOrWhiteSpace(callResult.Response.Reply))
                            {
                                var matchedRooms = new List<RoomDto>();

                                if (callResult.Response.RecommendedRoomIds.Any())
                                {
                                    var ids = callResult.Response.RecommendedRoomIds;

                                    // Linh hoạt số lượng thẻ phòng trả về:
                                    // 1. Nếu hỏi sâu 1 phòng hoặc người dùng yêu cầu 1 phòng cụ thể -> Đúng 1 thẻ
                                    // 2. Nếu so sánh giữa 2 phòng -> Đúng 2 thẻ
                                    // 3. Nếu tìm kiếm chung -> Tối đa 3 thẻ
                                    if (intent == UserChatIntent.SpecificRoomInquiry || Regex.IsMatch(question, @"\b(1|một)\s*(phòng|căn|nơi|chỗ)\b", RegexOptions.IgnoreCase))
                                    {
                                        ids = ids.Take(1).ToList();
                                    }
                                    else if (intent == UserChatIntent.RoomComparison)
                                    {
                                        ids = ids.Take(2).ToList();
                                    }
                                    else
                                    {
                                        ids = ids.Take(3).ToList();
                                    }

                                    var dbRooms = db.Phong_Tro
                                                    .Include(r => r.Khu_Vuc)
                                                    .Include(r => r.Hinh_Anh)
                                                    .Include(r => r.Loai_Tin)
                                                    .Where(r => ids.Contains(r.ID_Phong_Tro) && r.Trang_Thai == true)
                                                    .ToList();

                                    // Giữ thứ tự theo danh sách LLM recommend
                                    matchedRooms = ids
                                        .Select(id => dbRooms.FirstOrDefault(r => r.ID_Phong_Tro == id))
                                        .Where(r => r != null)
                                        .Select(r => MapToRoomDto(r))
                                        .ToList();
                                }
                                // Với các câu hỏi FAQ, Chào hỏi, Website -> matchedRooms rỗng, chỉ hiển thị Text

                                return Json(new
                                {
                                    success = true,
                                    reply = callResult.Response.Reply,
                                    rooms = matchedRooms,
                                    provider = callResult.ProviderName,
                                    model = callResult.ModelName,
                                    intent = intent.ToString()
                                });
                            }
                            else if (callResult != null)
                            {
                                failedCalls.Add(callResult);
                            }
                        }
                        catch (Exception provEx)
                        {
                            failedCalls.Add(new ProviderCallResult
                            {
                                ProviderName = provider.ProviderName,
                                ModelName = provider.ModelName,
                                ErrorMessage = provEx.Message
                            });
                        }
                    }
                }

                // =======================================================
                // 4. SMART EXPERT CONSULTATION ENGINE (OFFLINE / FALLBACK)
                // Chỉ chạy khi TẤT CẢ LLM provider đều fail
                // =======================================================
                if (intent == UserChatIntent.SpecificRoomInquiry && matchedCandidates.Any())
                {
                    var expertAdvice = AiConsultationEngine.GenerateDeepAdvice(intent, question, criteria);
                    return Json(new
                    {
                        success = true,
                        reply = expertAdvice.ReplyText,
                        rooms = matchedCandidates.Take(1).Select(MapToRoomDto).ToList(),
                        suggestions = expertAdvice.SuggestedQuestions,
                        provider = "Offline Expert Engine",
                        model = "Chuyên Gia Tư Vấn BĐS",
                        intent = intent.ToString()
                    });
                }

                if (intent == UserChatIntent.RoomComparison && matchedCandidates.Any())
                {
                    var expertAdvice = AiConsultationEngine.GenerateDeepAdvice(intent, question, criteria);
                    return Json(new
                    {
                        success = true,
                        reply = expertAdvice.ReplyText,
                        rooms = matchedCandidates.Take(2).Select(MapToRoomDto).ToList(),
                        suggestions = expertAdvice.SuggestedQuestions,
                        provider = "Offline Expert Engine",
                        model = "Chuyên Gia So Sánh Phòng",
                        intent = intent.ToString()
                    });
                }

                if (intent != UserChatIntent.RoomSearch)
                {
                    var expertAdvice = AiConsultationEngine.GenerateDeepAdvice(intent, question, criteria);
                    return Json(new
                    {
                        success = true,
                        reply = expertAdvice.ReplyText,
                        rooms = new List<RoomDto>(),
                        suggestions = expertAdvice.SuggestedQuestions,
                        provider = "Offline Expert Engine",
                        model = "Chuyên Gia Tư Vấn Thuê Trọ",
                        intent = intent.ToString()
                    });
                }

                // Nếu người dùng tìm kiếm phòng cụ thể (ROOM_SEARCH)
                var nlpResponse = ProcessSearchWithSmartNlp(criteria, matchedCandidates);
                return Json(new
                {
                    success = true,
                    reply = nlpResponse.Reply,
                    rooms = nlpResponse.Rooms,
                    provider = "Offline Expert Engine",
                    model = "Bộ Lọc CSDL Nâng Cao",
                    intent = intent.ToString()
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Chatbot Controller Error: " + ex.Message);
                return Json(new
                {
                    success = true,
                    reply = "Dạ chào bạn! Hiện tại hệ thống đang đồng bộ dữ liệu phòng trọ mới nhất. Bạn có thể nêu rõ khu vực muốn tìm (ví dụ: Cầu Giấy, Hoàn Kiếm, Đống Đa) và tầm giá dự kiến để mình hỗ trợ ngay nhé!",
                    rooms = new List<RoomDto>()
                });
            }
        }

        /// <summary>
        /// API kiểm tra sức khỏe của các Provider (GET /Chatbot/CheckApiStatus)
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> CheckApiStatus()
        {
            var results = await AiServiceManager.CheckAllProvidersHealthAsync();
            return Json(results, JsonRequestBehavior.AllowGet);
        }

        // =======================================================
        // BỘ LỌC TÌM KIẾM THÔNG MINH (ÁNH XẠ KHU VỰC VÀ GIÁ TIỀN CHUẨN XÁC)
        // =======================================================
        private List<Phong_Tro> FindMatchedRooms(ExtractedSearchCriteria criteria)
        {
            var query = db.Phong_Tro
                          .Include(p => p.Khu_Vuc)
                          .Include(p => p.Hinh_Anh)
                          .Include(p => p.Video)
                          .Include(p => p.Loai_Tin)
                          .Where(p => p.Trang_Thai == true);

            // 1. Lọc theo giá nếu có
            if (criteria.MinPrice.HasValue)
            {
                query = query.Where(p => p.Gia_Ca >= criteria.MinPrice.Value);
            }
            if (criteria.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Gia_Ca <= criteria.MaxPrice.Value);
            }

            // 2. Lọc theo tiện ích
            if (criteria.HasVideo) query = query.Where(p => p.Video.Any());
            if (criteria.HasGacXep) query = query.Where(p => p.Ten_Phong.Contains("gác") || p.Mo_Ta.Contains("gác"));
            if (criteria.HasBanCong) query = query.Where(p => p.Ten_Phong.Contains("ban công") || p.Mo_Ta.Contains("ban công"));
            if (criteria.HasFullNoiThat) query = query.Where(p => p.Ten_Phong.Contains("nội thất") || p.Mo_Ta.Contains("nội thất"));

            // 3. Sắp xếp thông minh theo phân khúc giá mong muốn của khách
            List<Phong_Tro> allPriceMatched;
            if (criteria.TargetPrice.HasValue)
            {
                decimal target = criteria.TargetPrice.Value;
                allPriceMatched = query.AsEnumerable()
                                       .OrderBy(p => Math.Abs((p.Gia_Ca ?? 0) - target))
                                       .ThenByDescending(p => p.Loai_Tin != null ? (p.Loai_Tin.CapDo ?? 0) : 0)
                                       .ToList();
            }
            else if (criteria.MaxPrice.HasValue)
            {
                allPriceMatched = query.OrderByDescending(p => p.Gia_Ca)
                                       .ThenByDescending(p => p.Loai_Tin != null ? (p.Loai_Tin.CapDo ?? 0) : 0)
                                       .ToList();
            }
            else
            {
                allPriceMatched = query.OrderBy(p => p.Gia_Ca).ToList();
            }

            // 4. Khớp khu vực với ánh xạ thông minh (Cầu Giấy -> Phường Láng, Giảng Võ, Ba Đình...)
            if (!string.IsNullOrEmpty(criteria.RawDistrict))
            {
                var wardKeywords = AiConsultationEngine.GetMatchingWardKeywords(criteria.RawDistrict);
                var exactDistrictList = new List<Phong_Tro>();

                foreach (var r in allPriceMatched)
                {
                    string rText = ((r.Khu_Vuc != null ? r.Khu_Vuc.Ten_KV : "") + " " + r.Dia_Chi + " " + r.Ten_Phong).ToLower();
                    string normRText = AiConsultationEngine.RemoveDiacritics(rText);

                    bool isMatch = false;
                    foreach (var kw in wardKeywords)
                    {
                        string normKw = AiConsultationEngine.RemoveDiacritics(kw.ToLower());
                        if (normRText.Contains(normKw))
                        {
                            isMatch = true;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        exactDistrictList.Add(r);
                    }
                }

                if (exactDistrictList.Count > 0)
                {
                    return exactDistrictList;
                }
            }

            return allPriceMatched;
        }

        private class NlpSearchResponse
        {
            public string Reply { get; set; }
            public List<RoomDto> Rooms { get; set; } = new List<RoomDto>();
        }

        private NlpSearchResponse ProcessSearchWithSmartNlp(ExtractedSearchCriteria criteria, List<Phong_Tro> matchedCandidates)
        {
            var res = new NlpSearchResponse();

            if (matchedCandidates != null && matchedCandidates.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("Tuyệt vời! Dựa trên yêu cầu của bạn");
                if (!string.IsNullOrEmpty(criteria.RawDistrict))
                {
                    if (criteria.RawDistrict.Equals("Cầu Giấy", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(" tại khu vực **Cầu Giấy và các phường giáp ranh** (như Phường Láng gần ĐH Ngoại Thương, Giảng Võ)");
                    }
                    else
                    {
                        sb.Append(" tại khu vực **").Append(criteria.RawDistrict).Append("**");
                    }
                }

                if (criteria.MinPrice.HasValue && criteria.MaxPrice.HasValue && criteria.MinPrice != criteria.MaxPrice)
                    sb.Append(" với tầm giá **từ ").Append(FormatCurrency(criteria.MinPrice.Value)).Append(" đến ").Append(FormatCurrency(criteria.MaxPrice.Value)).Append("**");
                else if (criteria.TargetPrice.HasValue)
                    sb.Append(" với tầm giá **khoảng ").Append(FormatCurrency(criteria.TargetPrice.Value)).Append("**");
                else if (criteria.MaxPrice.HasValue)
                    sb.Append(" với mức giá **dưới ").Append(FormatCurrency(criteria.MaxPrice.Value)).Append("**");

                var topRooms = matchedCandidates.Take(4).ToList();
                sb.Append(", mình đã chọn lọc được **").Append(topRooms.Count).Append(" phòng trọ phù hợp nhất**:\n\n");
                sb.Append("💡 *Mẹo cho bạn*: Các phòng này đều có giá thuê minh bạch đúng với ngân sách của bạn. Bạn có thể bấm **Xem chi tiết** để xem ảnh, video và số điện thoại liên hệ chủ nhà!");

                res.Reply = sb.ToString();
                res.Rooms = topRooms.Select(MapToRoomDto).ToList();
                return res;
            }

            // =======================================================
            // FALLBACK: NẾU KHÔNG CÓ PHÒNG NÀO KHỚP ĐỊA ĐIỂM
            // Gợi ý phòng có mức giá sát nhất với tài chính của khách
            // =======================================================
            decimal targetFallback = criteria.TargetPrice ?? criteria.MaxPrice ?? 3000000m;

            var fallback = db.Phong_Tro
                             .Include(p => p.Khu_Vuc)
                             .Include(p => p.Hinh_Anh)
                             .Include(p => p.Loai_Tin)
                             .Where(p => p.Trang_Thai == true)
                             .AsEnumerable()
                             .OrderBy(p => Math.Abs((p.Gia_Ca ?? 0) - targetFallback))
                             .Take(3)
                             .ToList();

            var sbFallback = new StringBuilder();
            sbFallback.Append("Hiện tại trong CSDL chưa có phòng nào khớp hoàn toàn với tiêu chí");
            if (!string.IsNullOrEmpty(criteria.RawDistrict)) sbFallback.Append(" tại **").Append(criteria.RawDistrict).Append("**");
            if (criteria.TargetPrice.HasValue) sbFallback.Append(" ở mức giá khoảng ").Append(FormatCurrency(criteria.TargetPrice.Value));
            else if (criteria.MaxPrice.HasValue) sbFallback.Append(" ở mức giá ").Append(FormatCurrency(criteria.MaxPrice.Value));
            sbFallback.Append(".\n\n👉 Mình xin gợi ý **các phòng trọ có mức giá sát nhất với tầm tài chính của bạn** trên hệ thống dưới đây:");

            res.Reply = sbFallback.ToString();
            res.Rooms = fallback.Select(MapToRoomDto).ToList();

            return res;
        }

        // =======================================================
        // HELPER FUNCTIONS
        // =======================================================
        /// <summary>
        /// Phát hiện các phòng trong CSDL được người dùng trích dẫn (theo ID hoặc theo Tiêu đề phòng)
        /// </summary>
        private List<Phong_Tro> DetectReferencedRoomsInDatabase(string question)
        {
            var matched = new List<Phong_Tro>();
            if (string.IsNullOrWhiteSpace(question)) return matched;

            // 1. Tìm theo ID: #23, id 23, phòng 23, căn 23
            var idMatches = Regex.Matches(question, @"(?:#|id\s*|phòng\s*|căn\s*)(\d+)", RegexOptions.IgnoreCase);
            var explicitIds = new List<int>();
            foreach (Match m in idMatches)
            {
                if (int.TryParse(m.Groups[1].Value, out int id) && !explicitIds.Contains(id))
                {
                    explicitIds.Add(id);
                }
            }

            if (explicitIds.Any())
            {
                var roomsById = db.Phong_Tro
                                  .Include(p => p.Khu_Vuc)
                                  .Include(p => p.Hinh_Anh)
                                  .Include(p => p.Video)
                                  .Include(p => p.Loai_Tin)
                                  .Where(p => explicitIds.Contains(p.ID_Phong_Tro) && p.Trang_Thai == true)
                                  .ToList();
                if (roomsById.Any())
                {
                    return roomsById;
                }
            }

            // 2. Tìm theo Tên / Tiêu đề phòng trong CSDL
            string cleanQ = AiConsultationEngine.RemoveDiacritics(question.ToLower())
                                                .Replace("–", "-")
                                                .Replace("—", "-");

            var allActiveRooms = db.Phong_Tro
                                   .Include(p => p.Khu_Vuc)
                                   .Include(p => p.Hinh_Anh)
                                   .Include(p => p.Video)
                                   .Include(p => p.Loai_Tin)
                                   .Where(p => p.Trang_Thai == true)
                                   .ToList();

            foreach (var room in allActiveRooms)
            {
                if (string.IsNullOrWhiteSpace(room.Ten_Phong)) continue;

                string cleanTitle = AiConsultationEngine.RemoveDiacritics(room.Ten_Phong.ToLower())
                                                       .Replace("–", "-")
                                                       .Replace("—", "-");

                // So khớp chính xác toàn bộ tiêu đề (bỏ qua dấu câu nhỏ)
                if (cleanQ.Contains(cleanTitle))
                {
                    if (!matched.Any(r => r.ID_Phong_Tro == room.ID_Phong_Tro))
                    {
                        matched.Add(room);
                    }
                    continue;
                }

                // So khớp cụm từ đặc trưng nếu tiêu đề có dấu gạch ngang
                var parts = cleanTitle.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string part0 = parts[0].Trim();
                    string part1 = parts[1].Trim();
                    if (part0.Length >= 8 && part1.Length >= 8 && cleanQ.Contains(part0) && cleanQ.Contains(part1))
                    {
                        if (!matched.Any(r => r.ID_Phong_Tro == room.ID_Phong_Tro))
                        {
                            matched.Add(room);
                        }
                    }
                }
            }

            return matched;
        }

        private static string BuildKeyAmenities(Phong_Tro p)
        {
            var list = new List<string>();
            string text = (p.Ten_Phong + " " + p.Mo_Ta).ToLower();

            if (p.Video != null && p.Video.Any()) list.Add("Video quay thực tế");
            if (text.Contains("ban công") || text.Contains("thoáng")) list.Add("Ban công thoáng mát");
            if (text.Contains("nội thất") || text.Contains("full đồ")) list.Add("Full nội thất");
            if (text.Contains("gác") || text.Contains("gác xép")) list.Add("Gác xép");
            if (text.Contains("khép kín")) list.Add("Khép kín");
            if (text.Contains("thang máy")) list.Add("Thang máy");
            if (text.Contains("điều hòa")) list.Add("Điều hòa");
            if (text.Contains("nóng lạnh")) list.Add("Nóng lạnh");
            if (text.Contains("an ninh") || text.Contains("camera") || text.Contains("khóa vân tay")) list.Add("An ninh cao");
            if (text.Contains("tự do") || text.Contains("không chung chủ")) list.Add("Giờ giấc tự do");

            return list.Count > 0 ? string.Join(", ", list.Take(4)) : "Cơ bản";
        }

        private RoomDto MapToRoomDto(Phong_Tro p)
        {
            string firstImg = p.Hinh_Anh.FirstOrDefault()?.Url_Anh;
            string imgUrl = !string.IsNullOrEmpty(firstImg)
                ? (firstImg.StartsWith("http") ? firstImg : "/Kho/Img/" + firstImg)
                : "/Assets/Images/banner-thuecanho.jpg";

            string priceStr = p.Gia_Ca.HasValue
                ? string.Format(new CultureInfo("vi-VN"), "{0:N0} đ/tháng", p.Gia_Ca.Value)
                : "Thỏa thuận";

            string areaStr = p.Dien_Tich.HasValue ? p.Dien_Tich.Value + " m²" : "Chưa cập nhật";

            return new RoomDto
            {
                id = p.ID_Phong_Tro,
                name = p.Ten_Phong,
                price = priceStr,
                area = areaStr,
                address = p.Dia_Chi ?? "",
                district = p.Khu_Vuc != null ? p.Khu_Vuc.Ten_KV : "",
                areaName = p.Loai_Tin != null ? p.Loai_Tin.Ten_LoaiTin : "Phòng trọ",
                imageUrl = imgUrl
            };
        }

        private static string FormatCurrency(decimal amount)
        {
            if (amount >= 1000000)
            {
                return (amount / 1000000m).ToString("0.#") + " triệu";
            }
            return amount.ToString("N0") + " đ";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
