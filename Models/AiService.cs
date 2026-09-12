using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuanLyPhongTro.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyPhongTro.Models
{
    public class AiRoomCatalogItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public double Area { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
        public bool HasVideo { get; set; }
        public string KeyAmenities { get; set; }
        public string Description { get; set; }
    }

    public class ChatHistoryItem
    {
        public string Role { get; set; } // "user" hoặc "model" / "assistant"
        public string Content { get; set; }
    }

    public class AiChatResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; }
        public List<int> RecommendedRoomIds { get; set; } = new List<int>();
        public List<string> SuggestedQuestions { get; set; } = new List<string>();
        public string UsedProvider { get; set; }
        public string UsedModel { get; set; }
    }

    public class ProviderCallResult
    {
        public bool Success { get; set; }
        public AiChatResponse Response { get; set; }
        public string ProviderName { get; set; }
        public string ModelName { get; set; }
        public int? HttpStatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsQuotaExceeded { get; set; }
        public bool IsInvalidKey { get; set; }
    }

    public class ProviderHealthDto
    {
        public string ProviderName { get; set; }
        public string ModelName { get; set; }
        public bool IsConfigured { get; set; }
        public string Status { get; set; }        // "Sẵn Sàng (Online)", "Hết Quota (429)", "Key Lỗi (401)", "Chưa Cấu Hình"
        public string DetailMessage { get; set; }
        public long LatencyMs { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Giao diện chuẩn cho tất cả các nhà cung cấp AI (Google Gemini, OpenAI, DeepSeek, Groq, Ollama...)
    /// </summary>
    public interface IAiProvider
    {
        string ProviderName { get; }
        string ModelName { get; }
        bool IsConfigured();
        Task<AiChatResponse> AskAsync(string userQuestion, List<AiRoomCatalogItem> candidateRooms, List<ChatHistoryItem> history = null);
        Task<ProviderCallResult> AskWithDiagnosticsAsync(string userQuestion, List<AiRoomCatalogItem> candidateRooms, List<ChatHistoryItem> history = null);
        Task<ProviderHealthDto> CheckHealthAsync();
    }

    /// <summary>
    /// 1. Provider Google Gemini (1.5 Flash, 2.0 Flash...)
    /// </summary>
    public class GeminiAiProvider : IAiProvider
    {
        public string ProviderName => "Google Gemini";
        public string ModelName => _modelName;
        private readonly List<string> _apiKeys;
        private readonly string _modelName;
        private static int _keyRotationCounter = 0;

        public GeminiAiProvider(string apiKeyString, string modelName = "gemini-3.6-flash")
        {
            _apiKeys = ParseKeys(apiKeyString);
            _modelName = string.IsNullOrWhiteSpace(modelName) ? "gemini-3.6-flash" : modelName.Trim();
        }

        private static List<string> ParseKeys(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(new[] { ',', ';', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(k => k.Trim())
                      .Where(k => !string.IsNullOrWhiteSpace(k) && !k.Contains("YOUR_") && k.Length > 15)
                      .Distinct()
                      .ToList();
        }

        public bool IsConfigured()
        {
            return _apiKeys != null && _apiKeys.Count > 0;
        }

        public async Task<AiChatResponse> AskAsync(string userQuestion, List<AiRoomCatalogItem> candidateRooms, List<ChatHistoryItem> history = null)
        {
            var res = await AskWithDiagnosticsAsync(userQuestion, candidateRooms, history);
            return res.Success ? res.Response : null;
        }

        public async Task<ProviderCallResult> AskWithDiagnosticsAsync(string userQuestion, List<AiRoomCatalogItem> candidateRooms, List<ChatHistoryItem> history = null)
        {
            var callResult = new ProviderCallResult
            {
                ProviderName = ProviderName,
                ModelName = _modelName
            };

            if (!IsConfigured())
            {
                callResult.ErrorMessage = "Chưa cấu hình API Key trong Web.config.";
                return callResult;
            }

            int totalKeys = _apiKeys.Count;
            // Round-robin index: Mỗi request tự động đổi sang key tiếp theo trong danh sách
            int startIndex = Math.Abs(System.Threading.Interlocked.Increment(ref _keyRotationCounter)) % totalKeys;

            string systemPrompt = AiServiceManager.BuildTokenEfficientPrompt(candidateRooms);
            string lastErrorMessage = null;

            // Xoay vòng và tự động Failover: nếu key hiện tại gặp 429 hoặc lỗi, tự động thử key tiếp theo trong Pool
            for (int i = 0; i < totalKeys; i++)
            {
                int keyIndex = (startIndex + i) % totalKeys;
                string currentKey = _apiKeys[keyIndex];
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={currentKey}";

                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(25);

                        var contentsList = new List<object>();
                        contentsList.Add(new
                        {
                            role = "user",
                            parts = new[] { new { text = "[HƯỚNG DẪN HỆ THỐNG]:\n" + systemPrompt } }
                        });
                        contentsList.Add(new
                        {
                            role = "model",
                            parts = new[] { new { text = "Dạ, mình đã nắm rõ toàn bộ vai trò và quy tắc tư vấn." } }
                        });

                        if (history != null && history.Count > 0)
                        {
                            var priorHistory = history
                                .Where(h => !string.IsNullOrWhiteSpace(h.Content) && !h.Content.Trim().Equals(userQuestion.Trim(), StringComparison.OrdinalIgnoreCase))
                                .ToList();
                            var recentHistory = priorHistory.Skip(Math.Max(0, priorHistory.Count - 4)).ToList();
                            foreach (var h in recentHistory)
                            {
                                string roleName = (h.Role == "user") ? "user" : "model";
                                contentsList.Add(new
                                {
                                    role = roleName,
                                    parts = new[] { new { text = h.Content } }
                                });
                            }
                        }

                        contentsList.Add(new
                        {
                            role = "user",
                            parts = new[] { new { text = userQuestion } }
                        });

                        var payload = new
                        {
                            contents = contentsList,
                            generationConfig = new
                            {
                                temperature = 0.35,
                                maxOutputTokens = 4096
                            }
                        };

                        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(url, content);
                        callResult.HttpStatusCode = (int)response.StatusCode;

                        if (!response.IsSuccessStatusCode)
                        {
                            string err = await response.Content.ReadAsStringAsync();
                            if (response.StatusCode == (HttpStatusCode)429 || err.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                callResult.IsQuotaExceeded = true;
                                lastErrorMessage = $"Key #{keyIndex + 1}: Hết hạn mức (429 Quota Exceeded).";
                            }
                            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || (response.StatusCode == HttpStatusCode.BadRequest && err.Contains("API_KEY_INVALID")))
                            {
                                callResult.IsInvalidKey = true;
                                lastErrorMessage = $"Key #{keyIndex + 1}: API Key không hợp lệ (401/403).";
                            }
                            else
                            {
                                lastErrorMessage = $"Key #{keyIndex + 1}: Lỗi ({response.StatusCode}): {err}";
                            }

                            // Nếu còn key khác trong pool -> Thử key tiếp theo ngay lập tức
                            if (totalKeys > 1 && i < totalKeys - 1)
                            {
                                continue;
                            }

                            callResult.ErrorMessage = totalKeys > 1
                                ? $"Tất cả {totalKeys} API Keys của Gemini đều lỗi. Chi tiết: {lastErrorMessage}"
                                : lastErrorMessage;
                            return callResult;
                        }

                        string json = await response.Content.ReadAsStringAsync();
                        var parsed = JObject.Parse(json);
                        string text = parsed["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            lastErrorMessage = $"Key #{keyIndex + 1} trả về chuỗi rỗng.";
                            if (totalKeys > 1 && i < totalKeys - 1)
                            {
                                continue;
                            }
                            callResult.ErrorMessage = lastErrorMessage;
                            return callResult;
                        }

                        var chatRes = AiServiceManager.ExtractRecommendedIds(text);
                        chatRes.UsedProvider = totalKeys > 1
                            ? $"{ProviderName} (Pool Key #{keyIndex + 1}/{totalKeys})"
                            : ProviderName;
                        chatRes.UsedModel = _modelName;

                        callResult.Success = true;
                        callResult.Response = chatRes;
                        return callResult;
                    }
                }
                catch (Exception ex)
                {
                    lastErrorMessage = $"Key #{keyIndex + 1}: Lỗi kết nối: {ex.Message}";
                    if (totalKeys > 1 && i < totalKeys - 1)
                    {
                        continue;
                    }
                    callResult.ErrorMessage = lastErrorMessage;
                    return callResult;
                }
            }

            callResult.ErrorMessage = lastErrorMessage ?? "Không thể kết nối tới Google Gemini.";
            return callResult;
        }

        public async Task<ProviderHealthDto> CheckHealthAsync()
        {
            var dto = new ProviderHealthDto
            {
                ProviderName = ProviderName,
                ModelName = _modelName,
                IsConfigured = IsConfigured()
            };

            if (!dto.IsConfigured)
            {
                dto.Status = "Chưa Cấu Hình";
                dto.DetailMessage = "Chưa điền GeminiApiKey trong Web.config";
                return dto;
            }

            int keyCount = _apiKeys.Count;
            var sw = Stopwatch.StartNew();
            var res = await AskWithDiagnosticsAsync("Chào bạn ngắn gọn 1 câu", new List<AiRoomCatalogItem>());
            sw.Stop();
            dto.LatencyMs = sw.ElapsedMilliseconds;

            string poolInfo = keyCount > 1 ? $" (Pool {keyCount} Keys)" : "";

            if (res.Success)
            {
                dto.Status = "Sẵn Sàng (Online)";
                dto.DetailMessage = $"Kết nối thành công{poolInfo}, phản hồi tốt ({dto.LatencyMs}ms).";
            }
            else if (res.IsQuotaExceeded)
            {
                dto.Status = "Hết Quota (429)";
                dto.DetailMessage = $"Toàn bộ {keyCount} Khóa API đã hết hạn mức token. Cần thêm API Key mới vào Web.config.";
            }
            else if (res.IsInvalidKey)
            {
                dto.Status = "Key Lỗi (401)";
                dto.DetailMessage = $"API Key không hợp lệ{poolInfo}. Vui lòng kiểm tra lại tại aistudio.google.com.";
            }
            else
            {
                dto.Status = "Lỗi Kết Nối";
                dto.DetailMessage = res.ErrorMessage;
            }

            return dto;
        }
    }

    /// <summary>
    /// 2. Provider chuẩn OpenAI REST API (OpenAI, DeepSeek, Groq, Ollama)
    /// </summary>
    public class OpenAiCompatibleProvider : IAiProvider
    {
        private readonly string _name;
        private readonly List<string> _apiKeys;
        private readonly string _baseUrl;
        private readonly string _model;
        private static int _keyRotationCounter = 0;

        public string ProviderName => _name;
        public string ModelName => _model;

        public OpenAiCompatibleProvider(string name, string apiKeyString, string baseUrl, string model)
        {
            _name = name;
            _baseUrl = (baseUrl ?? "").TrimEnd('/');
            _model = model;
            _apiKeys = ParseKeys(apiKeyString, name, _baseUrl);
        }

        private static List<string> ParseKeys(string raw, string name, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (name.ToLower().Contains("ollama") || baseUrl.Contains("localhost"))
                    return new List<string> { "" };
                return new List<string>();
            }

            return raw.Split(new[] { ',', ';', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(k => k.Trim())
                      .Where(k => !string.IsNullOrWhiteSpace(k) && !k.Contains("YOUR_") && (k.Length > 10 || name.ToLower().Contains("ollama")))
                      .Distinct()
                      .ToList();
        }

        public bool IsConfigured()
        {
            if (_name.ToLower().Contains("ollama") || _baseUrl.Contains("localhost"))
                return !string.IsNullOrWhiteSpace(_baseUrl);

            return _apiKeys != null && _apiKeys.Count > 0;
        }

        public async Task<AiChatResponse> AskAsync(string userQuestion, List<AiRoomCatalogItem> candidateRooms, List<ChatHistoryItem> history = null)
        {
            var res = await AskWithDiagnosticsAsync(userQuestion, candidateRooms, history);
            return res.Success ? res.Response : null;
        }

        public async Task<ProviderCallResult> AskWithDiagnosticsAsync(string userQuestion, List<AiRoomCatalogItem> candidateRooms, List<ChatHistoryItem> history = null)
        {
            var callResult = new ProviderCallResult
            {
                ProviderName = ProviderName,
                ModelName = _model
            };

            if (!IsConfigured())
            {
                callResult.ErrorMessage = $"Chưa cấu hình API Key cho {_name} trong Web.config.";
                return callResult;
            }

            int totalKeys = _apiKeys.Count > 0 ? _apiKeys.Count : 1;
            int startIndex = Math.Abs(System.Threading.Interlocked.Increment(ref _keyRotationCounter)) % totalKeys;

            string systemPrompt = AiServiceManager.BuildTokenEfficientPrompt(candidateRooms);
            string endpoint = $"{_baseUrl}/chat/completions";
            string lastErrorMessage = null;

            for (int i = 0; i < totalKeys; i++)
            {
                int keyIndex = (startIndex + i) % totalKeys;
                string currentKey = _apiKeys.Count > 0 ? _apiKeys[keyIndex] : "";

                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(15);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Antigravity-AI/1.0");
                        if (!string.IsNullOrWhiteSpace(currentKey))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", currentKey);
                        }

                        var messagesList = new List<object>();
                        messagesList.Add(new { role = "system", content = systemPrompt });

                        if (history != null && history.Count > 0)
                        {
                            var priorHistory = history
                                .Where(h => !string.IsNullOrWhiteSpace(h.Content) && !h.Content.Trim().Equals(userQuestion.Trim(), StringComparison.OrdinalIgnoreCase))
                                .ToList();
                            var recentHistory = priorHistory.Skip(Math.Max(0, priorHistory.Count - 4)).ToList();
                            foreach (var h in recentHistory)
                            {
                                string roleName = (h.Role == "user") ? "user" : "assistant";
                                messagesList.Add(new { role = roleName, content = h.Content });
                            }
                        }

                        messagesList.Add(new { role = "user", content = userQuestion });

                        var payload = new
                        {
                            model = _model,
                            messages = messagesList,
                            temperature = 0.35,
                            max_tokens = 2048
                        };

                        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(endpoint, content);
                        callResult.HttpStatusCode = (int)response.StatusCode;

                        if (!response.IsSuccessStatusCode)
                        {
                            string err = await response.Content.ReadAsStringAsync();
                            if (response.StatusCode == (HttpStatusCode)429 || err.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0 || err.IndexOf("rate_limit", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                callResult.IsQuotaExceeded = true;
                                lastErrorMessage = $"Key #{keyIndex + 1}: {_name} hết hạn mức API (429 Quota Exceeded).";
                            }
                            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                            {
                                callResult.IsInvalidKey = true;
                                lastErrorMessage = $"Key #{keyIndex + 1}: API Key không hợp lệ (401/403).";
                            }
                            else
                            {
                                lastErrorMessage = $"Key #{keyIndex + 1}: Lỗi ({response.StatusCode}): {err}";
                            }

                            if (totalKeys > 1 && i < totalKeys - 1)
                            {
                                continue;
                            }

                            callResult.ErrorMessage = totalKeys > 1
                                ? $"Tất cả {totalKeys} API Keys của {_name} đều lỗi. Chi tiết: {lastErrorMessage}"
                                : lastErrorMessage;
                            return callResult;
                        }

                        string json = await response.Content.ReadAsStringAsync();
                        var parsed = JObject.Parse(json);
                        string text = parsed["choices"]?[0]?["message"]?["content"]?.ToString();

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            lastErrorMessage = $"Key #{keyIndex + 1} trả về chuỗi rỗng.";
                            if (totalKeys > 1 && i < totalKeys - 1)
                            {
                                continue;
                            }
                            callResult.ErrorMessage = lastErrorMessage;
                            return callResult;
                        }

                        var result = AiServiceManager.ExtractRecommendedIds(text);
                        result.UsedProvider = totalKeys > 1
                            ? $"{ProviderName} (Pool Key #{keyIndex + 1}/{totalKeys})"
                            : ProviderName;
                        result.UsedModel = _model;

                        callResult.Success = true;
                        callResult.Response = result;
                        return callResult;
                    }
                }
                catch (Exception ex)
                {
                    lastErrorMessage = $"Key #{keyIndex + 1}: Lỗi kết nối: {ex.Message}";
                    if (totalKeys > 1 && i < totalKeys - 1)
                    {
                        continue;
                    }
                    callResult.ErrorMessage = lastErrorMessage;
                    return callResult;
                }
            }

            callResult.ErrorMessage = lastErrorMessage ?? $"Không thể kết nối tới {_name}.";
            return callResult;
        }

        public async Task<ProviderHealthDto> CheckHealthAsync()
        {
            var dto = new ProviderHealthDto
            {
                ProviderName = ProviderName,
                ModelName = _model,
                IsConfigured = IsConfigured()
            };

            if (!dto.IsConfigured)
            {
                dto.Status = "Chưa Cấu Hình";
                dto.DetailMessage = $"Chưa điền {_name}ApiKey trong Web.config";
                return dto;
            }

            int keyCount = _apiKeys.Count;
            var sw = Stopwatch.StartNew();
            var res = await AskWithDiagnosticsAsync("Chào bạn", new List<AiRoomCatalogItem>());
            sw.Stop();
            dto.LatencyMs = sw.ElapsedMilliseconds;

            string poolInfo = keyCount > 1 ? $" (Pool {keyCount} Keys)" : "";

            if (res.Success)
            {
                dto.Status = "Sẵn Sàng (Online)";
                dto.DetailMessage = $"Kết nối thành công{poolInfo}, phản hồi tốt ({dto.LatencyMs}ms).";
            }
            else if (res.IsQuotaExceeded)
            {
                dto.Status = "Hết Quota (429)";
                dto.DetailMessage = $"{_name}: Toàn bộ {keyCount} Khóa API đã hết hạn mức token. Cần thêm API Key mới vào Web.config.";
            }
            else if (res.IsInvalidKey)
            {
                dto.Status = "Key Lỗi (401)";
                dto.DetailMessage = $"{_name}: API Key không hợp lệ{poolInfo}. Vui lòng kiểm tra lại cấu hình.";
            }
            else
            {
                dto.Status = "Lỗi Kết Nối";
                dto.DetailMessage = res.ErrorMessage;
            }

            return dto;
        }
    }

    /// <summary>
    /// Trình quản lý LLM Prompt Template siêu tiết kiệm token và điều phối Multi-Provider AI
    /// </summary>
    public static class AiServiceManager
    {
        public static string BuildTokenEfficientPrompt(List<AiRoomCatalogItem> candidateRooms)
        {
            var sb = new StringBuilder();

            // ===== IDENTITY & EXPERTISE =====
            // ===== IDENTITY & EXPERTISE =====
            sb.Append("BẠN LÀ: Chuyên gia tư vấn BĐS cho thuê của QuanLyPhongTro.vn — nền tảng tìm kiếm phòng trọ, căn hộ tại Hà Nội.\n");
            sb.Append("PHONG CÁCH: Thân thiện, chu đáo, tư vấn am hiểu thực tế như một chuyên gia bản địa Hà Nội — không cứng nhắc, không máy móc.\n\n");

            // ===== QUY TẮC NHẬN DIỆN KHÁCH HÀNG =====
            sb.Append("QUY TẮC PHÂN TÍCH KHÁCH HÀNG:\n");
            sb.Append("• KHÔNG TỰ Ý MẶC ĐỊNH khách hàng là 'sinh viên' nếu họ chưa nhắc đến từ 'sinh viên' hoặc tên trường. Họ có thể là người đi làm, hộ gia đình, nhân viên văn phòng!\n");
            sb.Append("• Khi câu hỏi chung chung (vd: 'tìm cho tôi 1 phòng', 'có phòng nào không'): Mở đầu lịch sự, đề xuất 1 căn trung tâm/linh hoạt nhất kèm câu hỏi mở: 'Bạn đang tìm phòng để đi làm hay đi học quanh khu vực nào và ngân sách khoảng bao nhiêu?'.\n\n");

            // ===== THỊ TRƯỜNG HÀ NỘI (knowledge base) =====
            sb.Append("KIẾN THỨC BẢN ĐỒ & TRƯỜNG HỌC HÀ NỘI (BẮT BUỘC CHÍNH XÁC 100%, KHÔNG ĐƯỢC NHẦM LẪN):\n");
            sb.Append("• Cụm Đống Đa (Láng, Chùa Láng, Nguyễn Chí Thanh, Láng Hạ): Ngoại Thương (FTU), Ngoại Giao (DAV), Luật Hà Nội (HLU), Giao Thông Vận Tải (UTC), HV Hành Chính, Lao Động Xã Hội. Rất tiện cho dân văn phòng trục Láng Hạ - Nguyễn Chí Thanh - Thái Hà.\n");
            sb.Append("• Cụm Hai Bà Trưng / Hoàng Mai (Giải Phóng, Đại La, Bạch Mai, Kim Liên): Bách Khoa (HUST), Kinh Tế Quốc Dân (NEU), Xây Dựng (HUCE), ĐH Mở, KD&CN. [LƯU Ý CỰC KỲ QUAN TRỌNG: TUYỆT ĐỐI KHÔNG nhầm Bách Khoa, NEU, Xây Dựng sang khu vực đường Láng hay Cầu Giấy!].\n");
            sb.Append("• Cụm Cầu Giấy (Xuân Thủy, Cầu Giấy, Dịch Vọng, Nghĩa Đô): ĐH Quốc Gia Hà Nội (VNU), Sư Phạm (HNUE), Báo Chí (AJC), Thương Mại (TMU), Sân Khấu Điện Ảnh, Điện Lực, Du Lịch.\n");
            sb.Append("• Cụm Thanh Xuân / Hà Đông (Nguyễn Trãi, Trần Phú): ĐH Hà Nội (HANU), KHTN & KHXH&NV (VNU), Kiến Trúc (HAU), Bưu Chính Viễn Thông (PTIT), Học Viện An Ninh, Y Cổ Truyền.\n");
            sb.Append("• Cụm Nam/Bắc Từ Liêm: ĐH Công Nghiệp (HaUI), Mỏ Địa Chất, Học Viện Tài Chính (AOF), Tài Nguyên Môi Trường.\n");
            sb.Append("• Cụm Ba Đình / Tây Hồ: Đội Cấn, Liễu Giai, Kim Mã, Thụy Khuê, quanh Hồ Tây (tiện cho người nước ngoài, chuyên gia, người thích không gian thoáng đãng).\n");
            sb.Append("• MẶT BẰNG GIÁ THỰC TẾ: Phòng trọ SV 15-25m² (1,8-3,5tr) | Chung cư mini/Studio 25-40m² (4-6,5tr) | Căn hộ dịch vụ/1PN (6,5-10tr) | Căn 2-3PN (10-20tr).\n\n");

            // ===== WEBSITE FEATURES =====
            sb.Append("CHỨC NĂNG WEBSITE:\n");
            sb.Append("• ĐĂNG KÝ: Nhấn 'Đăng ký' → Họ tên, SĐT, Mật khẩu → Chọn loại TK (Người thuê/Chủ nhà)\n");
            sb.Append("• ĐĂNG NHẬP: Nhấn 'Đăng nhập' → SĐT + Mật khẩu đã đăng ký\n");
            sb.Append("• TÌM KIẾM: Trang chủ → Bộ lọc (khu vực, giá, diện tích) → Tìm kiếm\n");
            sb.Append("• YÊU THÍCH: Icon ❤️ trên mỗi tin → Xem lại tại 'Tin đã lưu' (cần đăng nhập)\n");
            sb.Append("• ĐĂNG TIN: Đăng nhập chủ nhà → Quản lý → 'Đăng tin mới' → Điền thông tin → Upload ảnh/video\n");
            sb.Append("• XEM CHI TIẾT: Click vào tin → Xem ảnh, video, mô tả, SĐT chủ nhà\n");
            sb.Append("• BLOG/HỎI ĐÁP/MẪU HỢP ĐỒNG: Menu Blog → Kinh nghiệm thuê trọ, mẫu HĐ chuẩn\n\n");

            // ===== ROOM DATA =====
            if (candidateRooms != null && candidateRooms.Count > 0)
            {
                sb.Append("CÁC PHÒNG PHÙ HỢP ĐANG CÓ TRÊN HỆ THỐNG ([#ID|Tên|Giá|Diện tích|Khu vực|Tiện ích|Mô tả]):\n");
                foreach (var r in candidateRooms)
                {
                    string priceFormatted = (r.Price >= 1000000)
                        ? (r.Price / 1000000m).ToString("0.#") + "tr"
                        : r.Price.ToString("N0") + "đ";

                    string videoTag = r.HasVideo ? " | CÓ VIDEO CLIP THỰC TẾ" : "";
                    string descSnippet = !string.IsNullOrWhiteSpace(r.Description)
                        ? " | MT: " + (r.Description.Length > 120 ? r.Description.Substring(0, 120) + "..." : r.Description).Replace("\r\n", " ").Replace("\n", " ")
                        : "";

                    sb.AppendFormat("[#{0}|{1}|{2}|{3}m²|{4}|{5}{6}{7}]\n",
                        r.Id,
                        r.Title,
                        priceFormatted,
                        r.Area,
                        r.District,
                        string.IsNullOrWhiteSpace(r.KeyAmenities) ? "Cơ bản" : r.KeyAmenities,
                        videoTag,
                        descSnippet);
                }

                sb.Append("\nQUY TẮC PHÂN TÍCH VÀ TRẢ LỜI CỦA CHUYÊN VIÊN TƯ VẤN (TẬP TRUNG LỢI ÍCH & GIÚP CHỦ NHÀ CHỐT KHÁCH):\n");

                sb.Append("1. KHI KHÁCH HỎI CÓ VIDEO XEM TRƯỚC KHÔNG (clip, video, xem thực tế):\n");
                sb.Append("   • Khẳng định vui vẻ: 'Dạ có ạ! Trên hệ thống QuanLyPhongTro.vn đã có các phòng được chủ nhà quay video clip thực tế 100% để bạn xem trước không gian chân thực mà không cần mất công đi lại.'\n");
                sb.Append("   • Giới thiệu các phòng có video trong danh sách bên trên (nêu tên, giá, khu vực, ưu điểm).\n");
                sb.Append("   • Hướng dẫn khách: 'Bạn chỉ cần click trực tiếp vào thẻ phòng bên dưới để xem video thực tế và liên hệ chủ nhà nhé!'\n");
                sb.Append("   • Cuối câu BẮT BUỘC ghi mã các phòng có video: [RECOMMENDED_IDS: id1, id2...]\n");
                sb.Append("   • TUYỆT ĐỐI KHÔNG NÓI 'tính năng video đang hoàn thiện' hay từ chối khách!\n\n");

                sb.Append("2. KHI KHÁCH HỎI TƯ VẤN CHUYÊN SÂU 1 PHÒNG CỤ THỂ (vd: 'phòng này có gì tốt', 'ưu điểm phòng #ID'):\n");
                sb.Append("   • Phân tích sâu sắc giá trị thực tế mang lại cho người thuê:\n");
                sb.Append("     - Không gian sống: thoáng đãng, ban công/cửa sổ (đón ánh sáng tự nhiên, gió trời thông thoáng, phơi đồ nhanh khô, bảo vệ sức khỏe).\n");
                sb.Append("     - Tiện nghi & kinh tế: đồ đạc sẵn có (tiết kiệm hàng triệu chi phí mua sắm ban đầu), diện tích so với mức giá thuê.\n");
                sb.Append("     - Vị trí & kết nối: gần trường học, chợ, trục đường chính thuận tiện đi học, đi làm.\n");
                sb.Append("     - An ninh & tự do: không chung chủ, giờ giấc tự do, an ninh đảm bảo.\n");
                sb.Append("   • Kêu gọi hành động: Khuyên khách bấm vào thẻ phòng bên dưới xem ảnh/video và liên hệ ngay chủ nhà đặt lịch xem phòng kẻo người khác thuê mất.\n");
                sb.Append("   • Cuối câu BẮT BUỘC ghi duy nhất 1 ID: [RECOMMENDED_IDS: id]\n\n");

                sb.Append("3. KHI KHÁCH HỎI SO SÁNH GIỮA CÁC PHÒNG (vd: 'Phòng A có gì tốt so với phòng B'):\n");
                sb.Append("   • Đặt lên bàn cân so sánh rõ ràng, sắc bén:\n");
                sb.Append("     - Vị trí & Di chuyển: Căn nào tiện đi đâu hơn (gần trường/tuyến đường nào).\n");
                sb.Append("     - Mức giá & Diện tích: Bài toán kinh tế, độ rộng rãi.\n");
                sb.Append("     - Không gian & Phong cách sống: Nêu nét đặc trưng riêng (vd: căn yên tĩnh có ban công thoáng vs căn trung tâm tiện nghi gần khu sầm uất).\n");
                sb.Append("     - Lời khuyên chốt: Căn A phù hợp cho ai (người thích yên tĩnh, thoáng gió...), Căn B phù hợp cho ai (người thích gần trung tâm tiện nghi...). Khuyên khách liên hệ xem thực tế cả 2 để quyết định.\n");
                sb.Append("   • Cuối câu BẮT BUỘC ghi cả 2 ID: [RECOMMENDED_IDS: idA, idB]\n\n");

                sb.Append("4. KHI KHÁCH YÊU CẦU TÌM 1 PHÒNG (vd: 'tìm 1 phòng', 'gợi ý 1 căn tốt nhất'):\n");
                sb.Append("   • Chọn duy nhất 1 căn phù hợp nhất, tư vấn sâu và cuối câu ghi DUY NHẤT 1 ID: [RECOMMENDED_IDS: id]\n\n");

                sb.Append("5. KHI KHÁCH TÌM PHÒNG CHUNG (theo khu vực, khoảng giá, tiện ích):\n");
                sb.Append("   • Chọn 2 đến 3 phòng tiêu biểu nhất, giới thiệu súc tích và cuối câu ghi: [RECOMMENDED_IDS: id1, id2]\n\n");

                sb.Append("6. KHI KHÔNG CÓ PHÒNG PHÙ HỢP TIÊU CHÍ (giá chênh lệch quá xa):\n");
                sb.Append("   • KHÔNG ghi [RECOMMENDED_IDS]. Thừa nhận trung thực và tư vấn mặt bằng giá thực tế để khách điều chỉnh bộ lọc.\n");
            }
            else
            {
                sb.Append("QUY TẮC TRẢ LỜI KHI KHÔNG CÓ THẺ PHÒNG (CHỈ TRẢ LỜI TEXT, TUYỆT ĐỐI KHÔNG GHI [RECOMMENDED_IDS]):\n");
                sb.Append("• Chào hỏi / Hỏi chung: Trả lời thân thiện, hỏi lại khách khu vực & ngân sách để bắt đầu tìm phòng.\n");
                sb.Append("• Hỏi về website (đăng ký, đăng nhập, đăng tin, yêu thích): Hướng dẫn từng bước rõ ràng.\n");
                sb.Append("• Hỏi FAQ (tiền cọc, điện nước, hợp đồng, lừa đảo, kinh nghiệm xem phòng): Tư vấn chuyên sâu chuẩn xác theo thực tế Hà Nội.\n");
                sb.Append("• KHÔNG tự ý bịa ra thẻ phòng khi không có phòng thực tế trong danh sách.\n");
            }

            sb.Append("\nĐỊNH DẠNG: Dùng emoji sinh động, ngắt đoạn rõ ràng, làm nổi bật thông tin bằng **bold**. Ngôn ngữ lịch sự, tận tâm, thôi thúc hành động tích cực giúp kết nối khách thuê và chủ nhà hiệu quả.\n");
            return sb.ToString();
        }

        public static AiChatResponse ExtractRecommendedIds(string text)
        {
            var ids = new List<int>();

            // 1. Ưu tiên: Tìm cú pháp chính thức [RECOMMENDED_IDS: id1, id2]
            var match = Regex.Match(text, @"\[RECOMMENDED_IDS:\s*([0-9,\s]+)\]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string idsStr = match.Groups[1].Value;
                text = text.Replace(match.Value, "").Trim();

                var parts = idsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p.Trim(), out int id)) ids.Add(id);
                }
            }

            // 2. Fallback: Tự động parse #ID từ text khi LLM đề cập phòng nhưng quên ghi [RECOMMENDED_IDS]
            // Ví dụ: "căn #116", "phòng #42", "tin #7", "#116 tại Phường..."
            if (!ids.Any())
            {
                var hashMatches = Regex.Matches(text, @"#(\d+)", RegexOptions.IgnoreCase);
                foreach (Match m in hashMatches)
                {
                    if (int.TryParse(m.Groups[1].Value, out int id) && !ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return new AiChatResponse
            {
                Success = true,
                Reply = text,
                RecommendedRoomIds = ids
            };
        }

        /// <summary>
        /// Tạo danh sách các Provider theo cấu hình trong Web.config
        /// </summary>
        public static List<IAiProvider> GetConfiguredProviders()
        {
            var list = new List<IAiProvider>();

            string activeProvider = ConfigurationManager.AppSettings["ActiveAiProvider"] ?? "Gemini";

            // 1. Google Gemini
            string geminiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            string geminiModel = ConfigurationManager.AppSettings["GeminiModel"] ?? "gemini-3.6-flash";
            var gemini = new GeminiAiProvider(geminiKey, geminiModel);

            // 2. OpenAI
            string openAiKey = ConfigurationManager.AppSettings["OpenAiApiKey"];
            string openAiModel = ConfigurationManager.AppSettings["OpenAiModel"] ?? "gpt-4o-mini";
            string openAiUrl = ConfigurationManager.AppSettings["OpenAiBaseUrl"] ?? "https://api.openai.com/v1";
            var openAi = new OpenAiCompatibleProvider("OpenAI", openAiKey, openAiUrl, openAiModel);

            // 3. DeepSeek
            string deepseekKey = ConfigurationManager.AppSettings["DeepSeekApiKey"];
            string deepseekModel = ConfigurationManager.AppSettings["DeepSeekModel"] ?? "deepseek-chat";
            string deepseekUrl = ConfigurationManager.AppSettings["DeepSeekBaseUrl"] ?? "https://api.deepseek.com/v1";
            var deepseek = new OpenAiCompatibleProvider("DeepSeek", deepseekKey, deepseekUrl, deepseekModel);

            // 4. Groq (Miễn phí & Cực nhanh)
            string groqKey = ConfigurationManager.AppSettings["GroqApiKey"];
            string groqModel = ConfigurationManager.AppSettings["GroqModel"] ?? "llama-3.3-70b-versatile";
            string groqUrl = ConfigurationManager.AppSettings["GroqBaseUrl"] ?? "https://api.groq.com/openai/v1";
            var groq = new OpenAiCompatibleProvider("Groq", groqKey, groqUrl, groqModel);

            // 5. Ollama / Local AI
            string ollamaUrl = ConfigurationManager.AppSettings["OllamaBaseUrl"] ?? "http://localhost:11434/v1";
            string ollamaModel = ConfigurationManager.AppSettings["OllamaModel"] ?? "llama3";
            var ollama = new OpenAiCompatibleProvider("Ollama (Local)", "", ollamaUrl, ollamaModel);

            switch (activeProvider.ToLower())
            {
                case "openai":
                    list.Add(openAi);
                    list.Add(gemini);
                    list.Add(deepseek);
                    list.Add(groq);
                    break;
                case "deepseek":
                    list.Add(deepseek);
                    list.Add(gemini);
                    list.Add(openAi);
                    list.Add(groq);
                    break;
                case "groq":
                    list.Add(groq);
                    list.Add(gemini);
                    list.Add(openAi);
                    list.Add(deepseek);
                    break;
                case "ollama":
                    list.Add(ollama);
                    list.Add(gemini);
                    break;
                default: // Gemini
                    list.Add(gemini);
                    list.Add(openAi);
                    list.Add(deepseek);
                    list.Add(groq);
                    break;
            }

            return list;
        }

        /// <summary>
        /// Kiểm tra sức khỏe toàn bộ các API Key cấu hình trong hệ thống
        /// </summary>
        public static async Task<List<ProviderHealthDto>> CheckAllProvidersHealthAsync()
        {
            var providers = GetConfiguredProviders();
            string activeProvider = ConfigurationManager.AppSettings["ActiveAiProvider"] ?? "Gemini";
            var list = new List<ProviderHealthDto>();

            foreach (var prov in providers)
            {
                var health = await prov.CheckHealthAsync();
                health.IsActive = string.Equals(prov.ProviderName, activeProvider, StringComparison.OrdinalIgnoreCase) ||
                                  (activeProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && prov.ProviderName.Contains("Gemini"));
                list.Add(health);
            }

            return list;
        }
    }
}
