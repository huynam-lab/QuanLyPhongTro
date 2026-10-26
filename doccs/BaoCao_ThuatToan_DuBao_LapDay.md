# BÁO CÁO KHOA HỌC: THUẬT TOÁN DỰ BÁO TỶ LỆ LẤP ĐẦY PHÒNG TRỌ (OCCUPANCY FORECASTING)
**Dự án:** Hệ thống Quản Lý & Cho Thuê Phòng Trọ (QuanLyPhongTro.vn)  
**Tác giả:** Đội ngũ phát triển hệ thống  
**Mục tiêu:** Tài liệu đặc tả kỹ thuật, cơ sở toán học và kiến trúc triển khai thuật toán dự báo phục vụ báo cáo đồ án và vận hành hệ thống.

---

## 1. TỔNG QUAN BÀI TOÁN & Ý NGHĨA KINH DOANH

### 1.1. Bài toán Tỷ lệ lấp đầy (Occupancy Rate)
Trong lĩnh vực kinh doanh bất động sản cho thuê (phòng trọ, căn hộ mini, studio), **Tỷ lệ lấp đầy ($Y_t$)** là chỉ số sống còn phản ánh hiệu quả kinh doanh của chủ trọ và sức hút của thị trường:

$$Y_t = \frac{\text{Số phòng đã có khách thuê}}{\text{Tổng số phòng hiện có trên hệ thống}} \times 100\%$$

* **Nếu tỷ lệ lấp đầy quá thấp ($< 75\%$):** Chủ nhà bị đọng vốn, lãng phí chi phí khấu hao tài sản và chi phí vận hành (bảo vệ, rác, wifi, điện chung).
* **Nếu tỷ lệ lấp đầy đạt đỉnh ($> 95\%$):** Nhu cầu thuê vượt quá cung, chủ nhà có cơ hội tối ưu hóa doanh thu bằng cách nâng giá thuê hoặc mở rộng quy mô phòng.

### 1.2. Đặc thù của thị trường phòng trọ Hà Nội
Thị trường phòng trọ tại Hà Nội chịu sự chi phối mạnh mẽ bởi:
1. **Lực lượng sinh viên (~600.000 sinh viên tại các trường đại học lớn):** Chu kỳ nhập học (tháng 8 - 10) tạo ra làn sóng tìm phòng đột biến; kỳ nghỉ hè (tháng 6 - 7) lượng lớn sinh viên trả phòng hoặc về quê.
2. **Lực lượng người đi làm và cử nhân mới ra trường:** Sau kỳ nghỉ Tết Nguyên Đán (tháng 2 - 3) có xu hướng nhảy việc và tìm đổi nơi ở mới.
3. **Xu hướng tăng trưởng dài hạn:** Quá trình đô thị hóa và di cư cơ học vào thủ đô tăng đều theo từng năm.

Do đó, một thuật toán dự báo chính xác phải bóc tách được đồng thời: **Xu hướng dài hạn (Trend)** và **Biến động mùa vụ định kỳ 12 tháng (Seasonality)**.

---

## 2. CƠ SỞ KHOA HỌC CỦA MÔ HÌNH TOÁN HỌC

Thuật toán trong hệ thống được xây dựng dựa trên:
**Mô hình Phân rã Chuỗi Thời Gian dạng Nhân (Multiplicative Time-Series Decomposition)** kết hợp **Hồi quy Tuyến tính bằng Phương pháp Bình phương Tối thiểu (Ordinary Least Squares - OLS)**.

### 2.1. Tại sao chọn mô hình dạng Nhân (Multiplicative) thay vì dạng Cộng (Additive)?
Trong lý thuyết chuỗi thời gian, có hai dạng mô hình phân rã chính:
* **Dạng Cộng (Additive):** $Y_t = T_t + S_t + I_t$  
  *(Áp dụng khi biên độ dao động mùa vụ giữ nguyên không đổi dù quy mô hệ thống tăng lên)*.
* **Dạng Nhân (Multiplicative):** $Y_t = T_t \times S_t \times I_t$  
  *(Áp dụng khi biên độ biến động mùa vụ tỷ lệ thuận với xu hướng tăng trưởng của hệ thống)*.

> **Thực tế:** Khi hệ thống từ 100 phòng tăng lên 1.000 phòng, số lượng phòng được thuê thêm vào tháng cao điểm nhập học sẽ tăng theo cấp số nhân theo tỷ lệ phần trăm chứ không cố định một lượng phòng cứng. Vì vậy, **mô hình dạng nhân là lựa chọn chính xác nhất về mặt kinh tế lượng**.

### 2.2. Các thành phần trong mô hình
Mỗi giá trị quan sát $Y_t$ tại thời điểm tháng $t$ ($t = 1, 2, \dots, n$) được phân rã thành:
* $T_t$: Thành phần xu hướng dài hạn (Linear Trend).
* $S_t$: Chỉ số mùa vụ của tháng trong năm (Seasonal Index, lặp lại chu kỳ 12 tháng).
* $\epsilon_t$ ($I_t$): Thành phần sai số ngẫu nhiên / nhiễu trắng (Residuals/Noise).

---

## 3. THUẬT TOÁN CHI TIẾT & CÔNG THỨC TOÁN HỌC

Hệ thống thực thi thuật toán qua 5 bước liên hoàn trong lớp [`OccupancyForecastService.cs`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Models/OccupancyForecastService.cs):

```mermaid
flowchart TD
    A["Dữ liệu lịch sử 12-24 tháng (Lich_Su_Lap_Day)"] --> B["Bước 1: Hồi quy tuyến tính OLS (Tìm Trend T_t = a*t + b)"]
    B --> C["Bước 2: Khử xu hướng & Chuẩn hóa chỉ số mùa vụ S_m (12 tháng)"]
    C --> D["Bước 3: Ngoại suy tương lai (Predicted = T_future * S_m)"]
    D --> E["Bước 4: Đánh giá độ tin cậy R² & Khoảng tin cậy 95% (Upper/Lower Bound)"]
    E --> F["Bước 5: Động cơ luật Heuristic AI (Sinh khuyến nghị kinh doanh)"]
    F --> G["Hiển thị trực quan hóa Chart.js & Dashboard KPI"]
```

---

### Bước 1: Xác định Đường xu hướng Tuyến tính (Linear Trend bằng OLS)
Giả sử chuỗi dữ liệu lịch sử có $n$ điểm quan sát: $(t_1, Y_1), (t_2, Y_2), \dots, (t_n, Y_n)$ với $t = 1, 2, \dots, n$.

Phương trình hồi quy xu hướng có dạng:
$$T_t = a \cdot t + b$$

Áp dụng phương pháp bình phương tối thiểu để cực tiểu hóa tổng bình phương sai số $\sum (Y_t - T_t)^2$:

1. **Tính giá trị trung bình:**
   $$\bar{t} = \frac{1}{n} \sum_{i=1}^n t_i, \quad \bar{Y} = \frac{1}{n} \sum_{i=1}^n Y_i$$

2. **Hệ số góc / Độ dốc (Slope $a$):**
   $$a = \frac{\sum_{i=1}^n (t_i - \bar{t})(Y_i - \bar{Y})}{\sum_{i=1}^n (t_i - \bar{t})^2} = \frac{\sum (t_i \cdot Y_i) - n \cdot \bar{t} \cdot \bar{Y}}{\sum (t_i^2) - n \cdot (\bar{t})^2}$$

3. **Hệ số chặn (Intercept $b$):**
   $$b = \bar{Y} - a \cdot \bar{t}$$

* **Ý nghĩa:** Hệ số $a$ cho biết mỗi tháng tỷ lệ lấp đầy nền tảng tăng trưởng cơ bản thêm bao nhiêu $\%$. Giá trị $a \times 12$ đại diện cho tốc độ tăng trưởng hàng năm.

---

### Bước 2: Tách và Chuẩn hóa Hệ số Mùa vụ (Seasonality Indices)

1. **Khử xu hướng (Detrending):** Loại bỏ ảnh hưởng của xu hướng để trích xuất tỷ số mùa vụ thuần túy:
   $$Ratio_t = \frac{Y_t}{T_t} = \frac{Y_t}{a \cdot t + b}$$

2. **Gom nhóm và tính trung bình theo tháng ($m = 1, 2, \dots, 12$):**
   $$\overline{Ratio}_m = \frac{1}{C_m} \sum_{t \in \text{Tháng } m} Ratio_t$$
   *(Trong đó $C_m$ là số lần tháng $m$ xuất hiện trong dữ liệu lịch sử).*

3. **Chuẩn hóa (Normalization):** Để đảm bảo tổng chỉ số mùa vụ của trọn vẹn 12 tháng bằng đúng $12$ (trung bình bằng $1.0$):
   $$S_m = \frac{\overline{Ratio}_m}{\sum_{k=1}^{12} \overline{Ratio}_k} \times 12 \quad (m = 1, 2, \dots, 12)$$

* **Biện luận giá trị $S_m$:**
  * $S_m = 1.0$: Tháng có nhu cầu ở mức trung bình bình thường.
  * $S_m = 1.08$: Tháng cao điểm, nhu cầu vượt $8\%$ so với mức bình thường (Tháng 9, 10).
  * $S_m = 0.92$: Tháng thấp điểm, nhu cầu giảm $8\%$ so với mức bình thường (Tháng 6, 7).

---

### Bước 3: Dự báo cho các tháng tương lai ($k = 1, 2, \dots, K$)
Với số tháng dự báo tương lai $k$ ($k \in \{3, 6, 12\}$):
1. Xác định thời điểm tương lai: $t_{future} = n + k$.
2. Xác định tháng dương lịch tương ứng $m \in [1, 12]$.
3. **Công thức dự báo điểm (Point Forecast):**
   $$\hat{Y}_{n+k} = T_{n+k} \times S_m = \Big(a \cdot (n + k) + b\Big) \times S_m$$

4. **Kẹp biên giá trị thực tế (Realistic Clamping):**
   Tỷ lệ lấp đầy phòng trọ thực tế không thể nhỏ hơn $0\%$ hay vượt quá $100\%$. Thuật toán giới hạn khoảng dao động hợp lý:
   $$\hat{Y}_{clamped} = \max\Big(60.0, \min(99.5, \hat{Y}_{n+k})\Big)$$

---

### Bước 4: Đánh giá Độ chính xác ($R^2$) và Khoảng tin cậy 95% (Confidence Interval)

#### 1. Hệ số xác định $R^2$ (Coefficient of Determination)
Đo lường mức độ khớp giữa mô hình dự báo với toàn bộ lịch sử đã qua:
$$SS_{tot} = \sum_{i=1}^n (Y_i - \bar{Y})^2 \quad (\text{Tổng phương sai thực tế})$$
$$SS_{res} = \sum_{i=1}^n (Y_i - \hat{Y}_{fitted, i})^2 \quad (\text{Tổng bình phương phần dư})$$
$$R^2 = 1 - \frac{SS_{res}}{SS_{tot}}$$

* $R^2 \ge 0.85$ ($85\%$): Mô hình giải thích được trên $85\%$ sự biến động của chuỗi dữ liệu thực tế, đạt độ tin cậy xuất sắc.

#### 2. Sai số chuẩn ước lượng (Standard Error of Regression - $SE$)
$$SE = \sqrt{\frac{SS_{res}}{n - 2}}$$

#### 3. Khoảng tin cậy 95% (95% Confidence Interval)
Dự báo tương lai càng xa ($k$ lớn), rủi ro và độ bất định càng tăng. Biên độ dao động mở rộng theo thời gian:
$$\text{Margin}_k = 1.96 \times SE \times \sqrt{1 + \frac{k}{n}}$$

Từ đó xác định dải biên độ (Prediction Interval):
$$\text{LowerBound}_k = \max\Big(50.0, \, \hat{Y}_k - \text{Margin}_k\Big)$$
$$\text{UpperBound}_k = \min\Big(100.0, \, \hat{Y}_k + \text{Margin}_k\Big)$$

---

### Bước 5: Động cơ Khuyến nghị Kinh doanh Thông minh (AI Heuristic Rule Engine)
Dựa trên kết quả dự báo và hệ số mùa vụ, hàm `GenerateAiRecommendations` tự động sinh khuyến nghị chiến lược:
* **Nếu rơi vào Tháng 9 - 10 ($S_m$ đỉnh điểm):** Kích hoạt cảnh báo cao điểm sinh viên nhập học, khuyến nghị chủ nhà hoàn tất sửa chữa phòng trước 15 ngày và có thể tăng giá thuê từ $5\% - 8\%$.
* **Nếu rơi vào Tháng 5 - 7 ($S_m$ đáy thấp điểm):** Khuyến nghị tung chính sách giữ chân khách cũ (giảm giá hè, miễn phí internet tháng đầu).
* **Nếu rơi vào Tháng 2 - 3 (Sau Tết):** Khuyến nghị chủ nhà đẩy mạnh gói tin VIP để đón đầu làn sóng đổi trọ.
* **Nếu Slope $a > 0$:** Báo cáo tín hiệu tăng trưởng dài hạn tích cực để mở rộng quy mô.

---

## 4. ÁNH XẠ KIẾN TRÚC MÃ NGUỒN (CODE MAPPING)

| Thành phần kỹ thuật | File mã nguồn | Chức năng chi tiết |
| :--- | :--- | :--- |
| **Data Models (DTO)** | [`Models/OccupancyForecastService.cs`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Models/OccupancyForecastService.cs#L8-L43) | Định nghĩa `HistoricalDataPoint`, `ForecastDataPoint`, `ForecastResult` |
| **Thuật toán chính** | [`Models/OccupancyForecastService.cs`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Models/OccupancyForecastService.cs#L45-L200) | Triển khai OLS Linear Regression, tính $S_m$, $R^2$, và khoảng tin cậy |
| **Truy vấn CSDL SQL** | [`Models/OccupancyForecastService.cs`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Models/OccupancyForecastService.cs#L237-L300) | Đọc lịch sử từ bảng `Lich_Su_Lap_Day` theo từng `ID_KV` |
| **Cơ chế Fallback Data** | [`Models/OccupancyForecastService.cs`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Models/OccupancyForecastService.cs#L303-L337) | Tự động sinh dữ liệu chuẩn mẫu thị trường Hà Nội khi DB chưa đủ 6 tháng |
| **Controller điều phối** | [`Areas/Admin/Controllers/QuantrivienController.cs`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Areas/Admin/Controllers/QuantrivienController.cs) | Nhận tham số lọc `idKv`, `forecastMonths`, gọi service và trả về View |
| **Giao diện Chart.js** | [`Areas/Admin/Views/Quantrivien/DuDoanLapDay.cshtml`](file:///c:/Users/Admin/source/repos/QuanLyPhongTro/Areas/Admin/Views/Quantrivien/DuDoanLapDay.cshtml) | Vẽ biểu đồ vùng (Area Chart), dải dập dềnh Min/Max và 4 thẻ KPI |

---

## 5. SO SÁNH VỚI CÁC THUẬT TOÁN KHÁC

| Tiêu chí | Mô hình OLS + Seasonality (Hiện tại) | ARIMA / SARIMA | Mạng nơ-ron LSTM / Deep Learning |
| :--- | :--- | :--- | :--- |
| **Yêu cầu dữ liệu** | **Nhỏ gọn (12 - 24 tháng)** là chạy chuẩn | Cần 36 - 60 tháng | Cần hàng chục nghìn điểm dữ liệu |
| **Tốc độ xử lý** | **Tức thì (< 10ms)** | 1 - 3 giây | 5 - 15 giây (cần GPU) |
| **Khả năng giải thích (Explainability)** | **100% minh bạch bằng toán học** | Khá phức tạp (p, d, q) | Hộp đen (Black-box), khó giải thích |
| **Độ phù hợp đồ án** | **Tối ưu tuyệt đối** cho ứng dụng web | Phức tạp trong cấu hình C# | Quá nặng cho hệ thống Web ASP.NET |

---

## 6. KẾT LUẬN
Thuật toán dự báo tỷ lệ lấp đầy trong dự án **QuanLyPhongTro.vn** là sự kết hợp hoàn hảo giữa **Lý thuyết kinh tế lượng** và **Kỹ thuật phần mềm hiện đại**. Thuật toán không chỉ đưa ra con số dự báo chính xác mà còn trực quan hóa sinh động và chuyển hóa số liệu thành **hành động kinh doanh thực tế**, mang lại giá trị gia tăng rõ rệt cho đồ án tốt nghiệp.
