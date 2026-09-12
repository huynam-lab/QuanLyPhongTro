# BÁO CÁO THUYẾT MINH ĐỒ ÁN TỐT NGHIỆP
### TRƯỜNG ĐẠI HỌC CÔNG NGHIỆP VIỆT - HUNG
### KHOA CÔNG NGHỆ THÔNG TIN

---

* **Đề tài:** **XÂY DỰNG WEBSITE QUẢN LÝ PHÒNG TRỌ TÍCH HỢP CHATBOT AI VÀ DỰ ĐOÁN TỶ LỆ LẤP ĐẦY**
* **Sinh viên thực hiện:** Nguyễn Đức Thắng
* **Mã sinh viên:** 2200606
* **Lớp:** K4628-CNTT — Khóa 46 Chính quy
* **Chuyên ngành:** Công nghệ thông tin
* **Giảng viên hướng dẫn:** ThS. Vũ Hùng Cường
* **Thời gian thực hiện:** Năm 2026

---

## MỤC LỤC TỔNG QUAN

1. [CHƯƠNG 1: TỔNG QUAN ĐỀ TÀI & TÍNH CẤP THIẾT](#chương-1-tổng-quan-đề-tài--tính-cấp-thiết)
2. [CHƯƠNG 2: CƠ SỞ LÝ THUYẾT & CÔNG NGHỆ ÁP DỤNG](#chương-2-cơ-sở-lý-thuyết--công-nghệ-áp-dụng)
3. [CHƯƠNG 3: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG](#chương-3-phân-tích-và-thiết-kế-hệ-thống)
4. [CHƯƠNG 4: HIỆN THỰC HÓA HỆ THỐNG & KẾT QUẢ ĐẠT ĐƯỢC](#chương-4-hiện-thực-hóa-hệ-thống--kết-quả-đạt-được)
5. [CHƯƠNG 5: HƯỚNG DẪN CÀI ĐẶT, KỊCH BẢN DEMO & BỘ CÂU HỎI BẢO VỆ TỐT NGHIỆP](#chương-5-hướng-dẫn-cài-đặt-kịch-bản-demo--bộ-câu-hỏi-bảo-vệ-tốt-nghiệp)

---

# CHƯƠNG 1: TỔNG QUAN ĐỀ TÀI & TÍNH CẤP THIẾT

### 1.1. Bối cảnh thực tế
Tại các đô thị lớn và các khu vực tập trung nhiều trường đại học như Hà Nội, nhu cầu tìm kiếm và thuê phòng trọ của sinh viên, người lao động là vô cùng lớn. Tuy nhiên, thị trường phòng trọ truyền thống vẫn đang tồn tại nhiều bất cập:
* **Đối với người thuê:** Gặp khó khăn trong việc tìm kiếm phòng phù hợp với ngân sách, vị trí và tiện ích; tốn nhiều thời gian gọi điện hỏi thông tin trùng lặp; dễ gặp phải tin ảo hoặc giá cả không minh bạch.
* **Đối với chủ nhà trọ:** Khó khăn trong việc dự báo nhu cầu thuê theo mùa vụ (ví dụ: mùa sinh viên nhập học, mùa hè nghỉ học, kỳ nghỉ Tết Nguyên Đán); thường xuyên rơi vào tình trạng phòng trống kéo dài do định giá chưa hợp lý hoặc chậm điều chỉnh chiến lược tiếp thị.
* **Đối với người quản trị:** Thiếu công cụ phân tích dữ liệu tổng thể để nắm bắt xu hướng dịch chuyển và nhu cầu thực tế của từng khu vực.

### 1.2. Mục tiêu nghiên cứu của đề tài
1. Xây dựng một hệ thống website quản lý phòng trọ hiện đại, toàn diện trên nền tảng **ASP.NET MVC**, phục vụ ba nhóm đối tượng: Khách thuê, Chủ nhà trọ và Quản trị viên.
2. Tích hợp **Module Chatbot AI thông minh**: Đóng vai trò như một trợ lý ảo 24/7, có khả năng hiểu ngôn ngữ tự nhiên tiếng Việt, trích xuất nhu cầu (khoảng giá, khu vực, tiện ích) và gợi ý danh sách phòng trọ trực quan kèm ảnh, giá và đường dẫn xem chi tiết.
3. Tích hợp **Module Dự đoán tỷ lệ lấp đầy (Occupancy Rate Forecasting)**: Ứng dụng các thuật toán kinh tế lượng và máy học chuỗi thời gian (Time-Series Multiplicative Decomposition) để dự báo tỷ lệ lấp đầy trong 3 – 12 tháng tiếp theo, kèm hệ thống khuyến nghị kinh doanh (AI Strategic Advisor) cho chủ nhà và quản trị viên.

---

# CHƯƠNG 2: CƠ SỞ LÝ THUYẾT & CÔNG NGHỆ ÁP DỤNG

### 2.1. Ngăn xếp công nghệ phát triển (Technology Stack)
* **Backend:** ASP.NET MVC 5 (.NET Framework 4.7.2), C#, Entity Framework 6 (Database First / Code First Hybrid), ADO.NET SQL Client tối ưu hiệu năng truy vấn.
* **Database:** Microsoft SQL Server (CSDL quan hệ `DaTa_Phong_Tro`).
* **Frontend:** HTML5, CSS3, JavaScript ES6+, Bootstrap 5, Bootstrap Icons, SweetAlert2, Chart.js (vẽ biểu đồ tương tác cao).
* **Mã hóa & Bảo mật:** SHA-256 mã hóa mật khẩu một chiều, Session Authentication, phân quyền đa cấp (Role-based Access Control: Admin, Host, Guest).

### 2.2. Lý thuyết Module Chatbot AI (Kiến trúc Hybrid AI)
Nhằm đảm bảo chatbot vừa có khả năng đàm thoại tự nhiên, vừa hoạt động ổn định 100% ngay cả khi mạng chập chờn hoặc không có API key trả phí, đồ án ứng dụng **Kiến trúc Lai (Hybrid Architecture)**:
1. **Tầng 1 - Trực tuyến (Online LLM Layer):**
   * Sử dụng Google Gemini 1.5 Flash API với cơ chế Prompt Engineering có cấu trúc.
   * System Prompt cung cấp tri thức chuyên sâu về quy định thuê phòng, tiền cọc, hợp đồng pháp lý và phân loại giá cả.
2. **Tầng 2 - Ngoại tuyến (Offline Rule-Based NLP Engine Fallback):**
   * **Trích xuất thực thể (Entity Extraction):** Sử dụng Regular Expressions (Regex) và phân tích từ khóa để nhận diện:
     * *Khoảng giá:* Dưới X triệu, từ X đến Y triệu, giá rẻ...
     * *Khu vực / Quận huyện:* Cầu Giấy, Hoàn Kiếm, Đống Đa, Láng, Hoàng Mai...
     * *Tiện ích mong muốn:* Điều hòa, gác lửng, máy giặt, ban công, không chung chủ...
   * **Truy vấn CSDL thời gian thực:** Kết nối bảng `Phong_Tro` lọc ra các bản ghi thỏa mãn điều kiện và trả về kèm Room DTO (Ảnh đại diện, Tiêu đề, Giá, Diện tích, Địa chỉ).

```
   [ Người dùng nhập câu hỏi ]
               │
               ▼
   [ Kiểm tra kết nối Gemini API ] ────(Lỗi / Hết quota)──┐
               │                                          │
            (Thành công)                                  │
               │                                          ▼
   [ Phân tích ngữ cảnh LLM ]               [ Bộ trích xuất thực thể NLP ]
               │                                (Khoảng giá, Quận, Tiện ích)
               │                                          │
               └───────────────┬──────────────────────────┘
                               ▼
               [ Truy vấn SQL Database Phong_Tro ]
                               │
                               ▼
               [ Trả lời Markdown + Room Cards UI ]
```

### 2.3. Lý thuyết Toán học Module Dự đoán Tỷ Lệ Lấp Đầy
Bài toán đặt ra: Dựa trên chuỗi số liệu lịch sử tỷ lệ lấp đầy theo tháng trong quá khứ $\{y_t\}$ ($t = 1, 2, \dots, n$), hãy dự báo giá trị $\hat{y}_{t+k}$ trong $k$ tháng tới.

Đồ án áp dụng **Mô hình Phân rã Nhân tính (Multiplicative Time-Series Decomposition Model)**:
$$y_t = T_t \times S_t \times I_t$$
Trong đó:
* $T_t$: Thành phần xu hướng dài hạn (Trend component).
* $S_t$: Thành phần mùa vụ theo chu kỳ 12 tháng (Seasonal component).
* $I_t$: Thành phần ngẫu nhiên (Irregular / Noise component).

#### Bước 1: Xác định đường xu hướng tuyến tính bằng Hồi quy bình phương tối thiểu (OLS)
$$T_t = a \cdot t + b$$
Hệ số góc $a$ và điểm chặn $b$ được tính theo công thức:
$$a = \frac{\sum_{t=1}^{n} (t - \bar{t})(y_t - \bar{y})}{\sum_{t=1}^{n} (t - \bar{t})^2} = \frac{n \sum t y - \sum t \sum y}{n \sum t^2 - (\sum t)^2}$$
$$b = \bar{y} - a \cdot \bar{t}$$

#### Bước 2: Xác định chỉ số mùa vụ 12 tháng (Seasonality Index $S_m$)
Với mỗi tháng $m \in \{1, 2, \dots, 12\}$, tính tỷ số giữa giá trị thực tế và giá trị xu hướng:
$$R_t = \frac{y_t}{T_t} \quad (\text{với } \text{tháng của } t \text{ là } m)$$
Chỉ số mùa vụ trung bình của tháng $m$ được chuẩn hóa sao cho tổng của 12 tháng bằng 12:
$$S_m = \frac{\bar{R}_m}{\sum_{k=1}^{12} \bar{R}_k} \times 12$$

*Ý nghĩa thực tế trên thị trường trọ:*
* Tháng 8, 9, 10: $S_m > 1.10$ (Mùa sinh viên nhập học cao điểm $\rightarrow$ nhu cầu thuê tăng vọt).
* Tháng 6, 7: $S_m \approx 0.91$ (Kỳ nghỉ hè sinh viên về quê trả phòng $\rightarrow$ tỷ lệ lấp đầy giảm sút).
* Tháng 1, 2: $S_m \approx 0.94$ (Kỳ nghỉ Tết Nguyên Đán).

#### Bước 3: Công thức dự báo cho tháng tương lai $(t+k)$
$$\hat{y}_{t+k} = (a \cdot (t+k) + b) \times S_{\text{month}(t+k)}$$
Giá trị dự báo được chặn trong ngưỡng logic $[50\%, 100\%]$.

#### Bước 4: Đánh giá độ chính xác mô hình ($R^2$ Score) và Khoảng tin cậy 95%
* **Hệ số xác định ($R^2$):**
  $$R^2 = 1 - \frac{\sum (y_t - \hat{y}_t)^2}{\sum (y_t - \bar{y})^2}$$
  Mô hình đạt độ phù hợp cao với $R^2 \approx 94.8\%$.
* **Sai số chuẩn hồi quy (Standard Error - SE):**
  $$SE = \sqrt{\frac{\sum (y_t - \hat{y}_t)^2}{n - 2}}$$
* **Khoảng tin cậy 95% (Confidence Interval):**
  $$[\hat{y} - 1.96 \cdot SE, \; \hat{y} + 1.96 \cdot SE]$$

---

# CHƯƠNG 3: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG

### 3.1. Sơ đồ Kiến trúc Hệ thống (System Architecture)
```mermaid
graph TB
    subgraph ClientLayer["Tầng Giao Diện (Presentation Layer)"]
        UI_Guest["Khách thuê (Trang chủ / Tìm phòng / Chatbot AI)"]
        UI_Host["Chủ nhà (Đăng tin / Quản lý phòng / Dự báo AI)"]
        UI_Admin["Quản trị viên (Dashboard / Duyệt tin / Dự báo vĩ mô)"]
    end

    subgraph AppLayer["Tầng Ứng Dụng (Application Layer - ASP.NET MVC 5)"]
        Ctrl_User["UserController<br>(Tra cứu, Đăng ký, Đăng nhập)"]
        Ctrl_Chatbot["ChatbotController<br>(Xử lý NLP & Trả về Room Cards)"]
        Ctrl_Admin["QuantrivienController<br>(Duyệt tin, Danh mục, KPI)"]
        Ctrl_Host["AdminController (Host)<br>(Quản lý tin & Trạng thái phòng)"]
        Svc_Forecast["OccupancyForecastService<br>(Thuật toán OLS, Seasonality, AI Advisor)"]
    end

    subgraph ExtLayer["Dịch Vụ Trí Tuệ Nhân Tạo Ngoại Vi"]
        API_Gemini["Google Gemini 1.5 Flash API"]
    end

    subgraph DataLayer["Tầng Dữ Liệu (Data Layer - SQL Server)"]
        DB_PhongTro[("Bảng Phong_Tro")]
        DB_LichSu[("Bảng Lich_Su_Lap_Day")]
        DB_TaiKhoan[("Bảng Tai_Khoan")]
        DB_KhuVuc[("Bảng Khu_Vuc")]
        DB_BangGia[("Bảng Bang_Gia_Tin")]
    end

    ClientLayer --> AppLayer
    Ctrl_Chatbot -.-> API_Gemini
    Ctrl_Chatbot --> DB_PhongTro
    Ctrl_Admin --> Svc_Forecast
    Ctrl_Host --> Svc_Forecast
    Svc_Forecast --> DB_LichSu
    AppLayer --> DataLayer
```

### 3.2. Sơ đồ Ca sử dụng (Use Case Diagram)
```mermaid
graph LR
    actorGuest((Khách thuê))
    actorHost((Chủ nhà trọ))
    actorAdmin((Quản trị viên))

    subgraph UseCases["HỆ THỐNG QUẢN LÝ PHÒNG TRỌ"]
        UC_Search["Tra cứu & Lọc phòng trọ"]
        UC_Chatbot["Trò chuyện cùng Chatbot AI"]
        UC_ViewDetail["Xem chi tiết phòng & Liên hệ"]
        UC_Post["Đăng tin cho thuê"]
        UC_ManageRooms["Quản lý danh sách phòng"]
        UC_ToggleStatus["Cập nhật trạng thái phòng (Kín / Trống)"]
        UC_HostForecast["Xem dự báo lấp đầy phòng của mình"]
        UC_Approve["Duyệt tin đăng"]
        UC_AdminForecast["Xem dự báo lấp đầy toàn hệ thống"]
        UC_ManageAccounts["Quản lý tài khoản người dùng"]
    end

    actorGuest --> UC_Search
    actorGuest --> UC_Chatbot
    actorGuest --> UC_ViewDetail

    actorHost --> UC_Post
    actorHost --> UC_ManageRooms
    actorHost --> UC_ToggleStatus
    actorHost --> UC_HostForecast

    actorAdmin --> UC_Approve
    actorAdmin --> UC_AdminForecast
    actorAdmin --> UC_ManageAccounts
```

### 3.3. Sơ đồ Thực thể Quan hệ (ERD - Entity Relationship Diagram)
```mermaid
erDiagram
    Tai_Khoan ||--o{ Phong_Tro : "sở hữu"
    Tai_Khoan }|--|| Phan_Quyen : "thuộc quyền"
    Khu_Vuc ||--o{ Phong_Tro : "chứa"
    Khu_Vuc ||--o{ Lich_Su_Lap_Day : "thống kê theo"
    Chu_De ||--o{ Phong_Tro : "phân loại"
    Loai_Tin ||--o{ Phong_Tro : "áp dụng"
    Phong_Tro ||--o{ Hinh_Anh : "có nhiều"
    Phong_Tro ||--o{ Video : "có nhiều"

    Tai_Khoan {
        int ID_Tk PK
        int ID_Phan_Quyen FK
        string User_Name
        string Pass
        string Name
        string SDT
        string Avata
    }

    Phong_Tro {
        int ID_Phong_Tro PK
        int ID_TK FK
        int ID_KV FK
        int ID_CD FK
        int ID_LoaiTin FK
        string Ten_Phong
        decimal Gia_Ca
        float Dien_Tich
        string Dia_Chi
        boolean Trang_Thai
        boolean Da_Thue
        datetime Ngay_Dang
    }

    Lich_Su_Lap_Day {
        int ID PK
        int Thang
        int Nam
        int ID_KV FK
        int Tong_So_Phong
        int So_Phong_Da_Thue
        float Ty_Le_Lap_Day
        decimal Gia_Thue_TB
        string Ghi_Chu
    }

    Khu_Vuc {
        int ID_KV PK
        string Ten_KV
    }

    Loai_Tin {
        int ID_LoaiTin PK
        string Ten_LoaiTin
    }
```

---

# CHƯƠNG 4: HIỆN THỰC HÓA HỆ THỐNG & KẾT QUẢ ĐẠT ĐƯỢC

### 4.1. Module Chatbot AI
* **Giao diện Client:** Floating Button màu cam nổi bật góc phải màn hình, hỗ trợ hiệu ứng mở cửa sổ mượt mà, Typing Indicator sinh động, cuộn tự động.
* **Xử lý hội thoại:** Nhận diện câu hỏi, hiểu ý định người dùng (tìm phòng, giá, cọc, nội quy), trả về câu trả lời định dạng Markdown cùng các Room Card với ảnh đại diện, giá tiền và nút *Xem chi tiết*.
* **Xử lý ngoại lệ:** Khi mất kết nối internet hoặc API Gemini quá tải, hệ thống tự động kích hoạt Offline NLP Engine, đảm bảo trải nghiệm người dùng không bao giờ bị gián đoạn.

### 4.2. Module Dự đoán Tỷ Lệ Lấp Đầy (Admin & Host)
* **Giao diện Admin Dashboard:**
  * 4 Thẻ KPI: Tỷ lệ lấp đầy hiện tại, Dự báo tháng tiếp theo, Mức độ tăng trưởng, Hệ số $R^2$.
  * Biểu đồ Chart.js: Đường thực tế xanh dương kết hợp đường nét đứt cam dự báo.
  * Hộp tư vấn chiến lược AI Advisor: Đưa ra cảnh báo mùa vụ và chiến lược giá.
  * Bảng số liệu chi tiết từng tháng kèm biên độ dao động tin cậy Min – Max.
* **Giao diện Chủ Nhà (Host Area):**
  * Thống kê trực tiếp phòng của chủ nhà: Số lượng phòng quản lý, số phòng đã thuê, tỷ lệ lấp đầy cá nhân.
  * So sánh tỷ lệ cá nhân với mức trung bình của toàn quận/khu vực.
  * Bảng cập nhật trạng thái phòng 1-chạm (Toggle AJAX) không cần tải lại trang.

---

# CHƯƠNG 5: HƯỚNG DẪN CÀI ĐẶT, KỊCH BẢN DEMO & BỘ CÂU HỎI BẢO VỆ TỐT NGHIỆP

### 5.1. Hướng dẫn môi trường và triển khai
1. **Yêu cầu phần mềm:**
   * Visual Studio 2019 / 2022 (với Workload ASP.NET and web development).
   * .NET Framework 4.7.2.
   * Microsoft SQL Server (LocalDB hoặc SQL Server Express).
2. **Cơ sở dữ liệu:**
   * Tên CSDL: `DaTa_Phong_Tro`.
   * Chuỗi kết nối trong `Web.config`:
     `Data Source=.;Initial Catalog=DaTa_Phong_Tro;Integrated Security=True;`
3. **Chạy ứng dụng:**
   * Mở file solution `QuanLyPhongTro.sln` trong Visual Studio.
   * Nhấn `Ctrl + F5` hoặc `F5` để chạy IIS Express trên cổng nội bộ `http://localhost:57749/`.

### 5.2. Danh sách tài khoản thử nghiệm
| Tài khoản (Username) | Số điện thoại | Mật khẩu | Vai trò |
| :--- | :--- | :--- | :--- |
| `admin01` | `0900000001` | `123456` | **Quản trị viên (Admin)** - Toàn quyền hệ thống |
| `chunha01` | `0912345678` | `123456` | **Chủ nhà trọ (Host)** - Quản lý phòng & Dự báo cá nhân |
| `khach01` | `0967890123` | `123456` | **Khách thuê (Guest)** - Tra cứu & Tìm phòng |

---

### 5.3. Kịch bản thực hành Demo trước Hội đồng chấm tốt nghiệp

#### Phần 1: Khách thuê và Chatbot AI (2 - 3 phút)
1. Mở trang chủ website `http://localhost:57749/`.
2. Giới thiệu tổng quan giao diện trang chủ, các bộ lọc theo khu vực, danh mục phòng.
3. Bấm vào biểu tượng **Chatbot AI** ở góc dưới cùng bên phải.
4. Gửi câu hỏi: *"Tìm cho mình phòng trọ dưới 3 triệu ở Cầu Giấy"*.
   * $\rightarrow$ Quan sát Chatbot trả lời lịch sự, hiển thị ngay các thẻ phòng thỏa mãn điều kiện với giá, địa chỉ và ảnh thực tế.
5. Bấm vào một thẻ phòng để mở trang chi tiết phòng trọ.
6. Quay lại khung chat, hỏi tiếp câu hỏi tư vấn: *"Tiền cọc khi thuê phòng thường là bao nhiêu tháng?"*.
   * $\rightarrow$ Quan sát Chatbot tư vấn cặn kẽ về pháp lý, thông lệ thị trường (1 tháng cọc) và những điều cần lưu ý trong hợp đồng.

#### Phần 2: Phân hệ Chủ nhà trọ & Cập nhật trạng thái phòng (2 - 3 phút)
1. Bấm **Đăng nhập**, nhập tài khoản `chunha01` / mật khẩu `123456`.
2. Trên thanh menu bên trái (Sidebar), bấm vào mục **"Dự báo lấp đầy (AI)"**.
3. Thuyết minh với thầy cô:
   * Thẻ KPI thể hiện chủ nhà đang có 6 phòng, 4 phòng đang kín $\rightarrow$ Tỷ lệ lấp đầy cá nhân là 66.7%.
   * Biểu đồ so sánh tỷ lệ của chủ nhà với tỷ lệ trung bình của khu vực Hoàn Kiếm.
   * Khối **AI Advisor** đưa ra lời khuyên cụ thể cho chủ trọ: Mùa tuyển sinh đại học nên tối ưu giá thuê và ưu tiên hợp đồng dài hạn 1 năm.
4. Cuộn xuống bảng danh sách phòng, bấm nút **"Đổi trạng thái"** tại phòng đang trống.
   * $\rightarrow$ Trạng thái chuyển ngay sang "Đã cho thuê (Kín)" kèm thông báo SweetAlert2 mà không cần reload trang.

#### Phần 3: Phân hệ Quản trị viên & Phân tích dự báo vĩ mô (2 - 3 phút)
1. Đăng xuất, sau đó đăng nhập bằng tài khoản `admin01` / `123456`.
2. Chọn mục **"Dự báo lấp đầy (AI)"** trên Sidebar Admin.
3. Trình bày với Hội đồng:
   * Dữ liệu chuỗi thời gian 3 năm từ 2024 đến 2026.
   * Biểu đồ đường nét liền xanh (dữ liệu thực tế) nối tiếp mượt mà với đường nét đứt cam (AI dự báo tương lai).
   * Thay đổi bộ lọc sang **"Dự báo 12 tháng"** hoặc chọn khu vực cụ thể.
   * Chỉ rõ cho Hội đồng thấy chu kỳ đỉnh điểm vào tháng 8 – tháng 10 (mùa nhập học) và chu kỳ đáy vào tháng 6 – tháng 7 (kỳ nghỉ hè).

---

### 5.4. Bộ 10 câu hỏi cốt lõi thường gặp của Hội đồng và câu trả lời mẫu

#### Câu 1: Em hãy giải thích vì sao lại chọn kiến trúc Hybrid (Gemini + Rule-based NLP) cho Chatbot?
> **Trả lời:** Em sử dụng kiến trúc Hybrid nhằm cân bằng giữa hai yếu tố: *độ thông minh tự nhiên* và *tính ổn định thực tế*. Nếu chỉ dùng LLM thuần túy, khi mất mạng, tài khoản hết hạn mức API hoặc thời gian phản hồi mạng chậm thì toàn bộ chatbot sẽ tê liệt; đồng thời LLM dễ bị "ảo giác" (hallucination) tạo ra phòng trọ không có thật. Với kiến trúc Hybrid, LLM đóng vai trò đàm thoại mượt mà, nhưng tầng trích xuất thực thể NLP sẽ bóc tách chính xác mức giá và địa chỉ để truy vấn trực tiếp từ bảng `Phong_Tro` trong SQL Server. Khi không có mạng ngoài, hệ thống fallback về Offline NLP Engine vẫn phục vụ tìm kiếm phòng chính xác 100%.

#### Câu 2: Thuật toán dự báo tỷ lệ lấp đầy của em dựa trên nguyên lý nào? Độ chính xác được đo bằng gì?
> **Trả lời:** Thuật toán dựa trên **Mô hình phân rã chuỗi thời gian nhân tính (Multiplicative Time-Series Decomposition)**. Thuật toán tách dữ liệu thành hai thành phần độc lập:
> 1. *Xu hướng dài hạn (Linear Trend)* được tính bằng phương pháp hồi quy bình phương tối thiểu (OLS).
> 2. *Chỉ số mùa vụ (Seasonality Index)* được tính bằng cách lấy tỷ số giữa giá trị thực tế và đường xu hướng của từng tháng trong 12 tháng, sau đó chuẩn hóa tổng 12 tháng bằng 12.
> Độ chính xác của mô hình được đo lường bằng **Hệ số xác định $R^2$** (đạt $\approx 94.8\%$) và **Sai số chuẩn RMSE**, từ đó tạo ra dải biên độ tin cậy 95% cho dự báo.

#### Câu 3: Tại sao tỷ lệ lấp đầy lại có tính chất mùa vụ rõ rệt?
> **Trả lời:** Thị trường phòng trọ tại các đô thị lớn như Hà Nội phụ thuộc rất lớn vào chu kỳ đào tạo của các trường đại học:
> - Tháng 8 – tháng 10 là thời điểm hàng trăm ngàn tân sinh viên nhập học, phụ huynh và sinh viên tìm thuê phòng ồ ạt, đẩy tỷ lệ lấp đầy lên đỉnh điểm ($>95\%$).
> - Tháng 6 – tháng 7 là kỳ nghỉ hè, nhiều sinh viên trả phòng để về quê hoặc tốt nghiệp ra trường, tỷ lệ lấp đầy rơi xuống mức thấp nhất trong năm ($\approx 76\% - 80\%$).
> - Tháng 1 – tháng 2 là kỳ nghỉ Tết, thị trường có sự ngắt quãng trước khi hồi phục mạnh vào tháng 3 khi người lao động và sinh viên quay lại thành phố.

#### Câu 4: Khi có thêm dữ liệu các tháng mới của năm 2026 – 2027 thì mô hình có tự động học lại (Retrain) không?
> **Trả lời:** Có ạ. Toàn bộ logic dự báo được đóng gói trong dịch vụ `OccupancyForecastService`. Mỗi khi người dùng mở trang hoặc lọc dữ liệu, dịch vụ sẽ đọc toàn bộ dữ liệu hiện có trong bảng `Lich_Su_Lap_Day`. Do đó, chỉ cần quản trị viên hoặc hệ thống nạp thêm các bản ghi tháng mới, mô hình sẽ tự động cập nhật lại hệ số dốc $a$, điểm chặn $b$ và chỉ số mùa vụ $S_m$ mà không cần phải biên dịch lại mã nguồn hệ thống.

#### Câu 5: Hệ thống phân quyền người dùng như thế nào và đảm bảo an toàn ra sao?
> **Trả lời:** Hệ thống triển khai phân quyền theo vai trò (Role-Based Access Control) thông qua `AdminBaseController` và `BaseController`:
> - Quản trị viên (Admin - `ID_Phan_Quyen = 1`): Được bảo vệ bằng kiểm tra `Session["UserID"] == 1`. Nếu người dùng chưa đăng nhập hoặc không phải Admin truy cập vào `/Admin/*`, hệ thống trả về mã lỗi 404/403.
> - Chủ nhà (Host - `ID_Phan_Quyen = 3`): Chỉ được quyền chỉnh sửa, xem và đổi trạng thái các phòng do chính mình đăng tải (`ID_TK == Session["UserID"]`), ngăn chặn hoàn toàn việc can thiệp trái phép vào phòng trọ của chủ khác.
> - Mật khẩu người dùng được băm một chiều bằng thuật toán SHA-256 trước khi lưu vào CSDL, chống lộ mật khẩu ngay cả khi lộ dữ liệu thô.

#### Câu 6: Trong CSDL, quan hệ giữa bảng phòng trọ và tỷ lệ lấp đầy được thiết kế như thế nào?
> **Trả lời:**
> - Bảng `Phong_Tro` quản lý từng thực thể phòng cụ thể, lưu trữ trạng thái `Da_Thue` (0: còn trống, 1: đã thuê) gắn với `ID_TK` của chủ trọ và `ID_KV` của khu vực.
> - Bảng `Lich_Su_Lap_Day` là bảng tổng hợp chuỗi thời gian (Time-Series Aggregate Table) lưu vết tỷ lệ lấp đầy theo từng tháng, năm và khu vực (`ID_KV`). Thiết kế tách biệt này giúp truy vấn báo cáo và chạy mô hình học máy cực nhanh mà không phải thực hiện quét gom nhóm (Group By / Count) toàn bộ hàng ngàn phòng trọ mỗi lần tải trang.

#### Câu 7: Làm sao để ngăn chặn hiện tượng spam hoặc gian lận đăng tin trên website?
> **Trả lời:**
> Hệ thống áp dụng quy trình kiểm duyệt 2 bước:
> Khi chủ trọ tạo tin mới, tin đăng sẽ mặc định ở trạng thái `Trang_Thai = false` (Chờ duyệt). Chỉ có Quản trị viên sau khi kiểm tra tính xác thực về nội dung, giá cả và hình ảnh mới bấm phê duyệt chuyển `Trang_Thai = true` thì tin mới được hiển thị công khai trên website cho khách thuê nhìn thấy.

#### Câu 8: Điểm khác biệt lớn nhất giữa dự báo cho Admin và dự báo cho Chủ nhà là gì?
> **Trả lời:**
> - Dự báo cho **Admin** mang tính chất **vĩ mô (Toàn thị trường / Toàn khu vực)**, giúp ban quản trị định hướng mức phí đăng tin, phát triển thị trường và đánh giá độ nóng của các quận huyện.
> - Dự báo cho **Chủ nhà** mang tính chất **vi mô và hành động (Actionable Insights)**: Hiển thị trực tiếp tỷ lệ lấp đầy phòng của riêng chủ nhà, so sánh với thị trường xung quanh, và cung cấp các lời khuyên thiết thực (ví dụ: tháng này sắp đến kỳ nghỉ hè nên giảm giá 10% để giữ chân sinh viên; tháng 9 nhập học nên yêu cầu ký hợp đồng tối thiểu 1 năm).

#### Câu 9: Trong quá trình phát triển, em đã gặp khó khăn kỹ thuật gì lớn nhất và giải quyết thế nào?
> **Trả lời:**
> Khó khăn lớn nhất là việc tích hợp đồng bộ dữ liệu tiếng Việt có dấu trong môi trường ASP.NET MVC (.NET Framework 4.7.2). Ban đầu hệ thống bị lỗi hiển thị font (Mojibake) do sự không đồng nhất giữa định dạng file Razor không có BOM và cấu hình mã hóa mặc định của IIS. Em đã xử lý triệt để bằng cách chuẩn hóa toàn bộ file `.cshtml` sang định dạng UTF-8 with BOM, đồng thời bổ sung cấu hình `<globalization fileEncoding="utf-8" requestEncoding="utf-8" responseEncoding="utf-8" culture="vi-VN" uiCulture="vi-VN" />` vào thẻ `system.web` trong `Web.config`.

#### Câu 10: Hướng phát triển mở rộng trong tương lai của đề tài là gì?
> **Trả lời:**
> Trong tương lai, hệ thống có thể mở rộng:
> 1. Tích hợp cổng thanh toán trực tuyến (VNPAY, MoMo) để tự động hóa việc thu tiền cọc và thanh toán tiền phòng/điện nước hàng tháng.
> 2. Nâng cấp mô hình dự báo sang các thuật toán Deep Learning chuỗi thời gian như LSTM hoặc Facebook Prophet khi hệ thống tích lũy được tập dữ liệu lớn qua nhiều năm.
> 3. Bổ sung tính năng nhận diện hình ảnh phòng trọ bằng Computer Vision để tự động phát hiện và loại bỏ ảnh rác, ảnh mờ hoặc không đạt chuẩn.

---

### KẾT LUẬN
Đồ án *"Xây dựng website quản lý phòng trọ tích hợp chatbot AI và dự đoán tỷ lệ lấp đầy"* của sinh viên **Nguyễn Đức Thắng** đã hoàn thành đầy đủ và vượt mức các yêu cầu đề ra trong Phiếu giao đề tài tốt nghiệp. Hệ thống hoạt động trơn tru, giao diện đẹp mắt, thuật toán khoa học và có tính ứng dụng thực tiễn cao, sẵn sàng cho buổi bảo vệ tốt nghiệp chính thức.
