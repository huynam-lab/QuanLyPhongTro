// Đảm bảo code chỉ chạy sau khi toàn bộ HTML đã được tải (DOMContentLoaded)
document.addEventListener('DOMContentLoaded', () => {
    // --- Khởi tạo các phần tử ---
    const chatButton = document.getElementById('chatButton');
    const closeChatButton = document.getElementById('closeChatButton');
    const chatSidebar = document.getElementById('chatSidebar');
    const scrollTopButton = document.getElementById('scrollToTopButton');

    // Tạo Overlay (phần nền mờ) bằng JavaScript
    const overlay = document.createElement('div');
    overlay.className = 'pt-overlay';
    document.body.appendChild(overlay);


    // --- Logic Chat Sidebar ---

    // Hàm mở Sidebar
    function openChat() {
        if (chatSidebar) {
            chatSidebar.classList.add('open');
            overlay.classList.add('open');
        } else {
            console.error('Lỗi: Không tìm thấy phần tử #chatSidebar. Vui lòng kiểm tra HTML.');
        }
    }

    // Hàm đóng Sidebar
    function closeChat() {
        if (chatSidebar) {
            chatSidebar.classList.remove('open');
            overlay.classList.remove('open');
        }
    }

    // Gắn sự kiện (chỉ khi tìm thấy các phần tử)
    if (chatButton) {
        chatButton.addEventListener('click', openChat);
    } else {
        console.warn('Cảnh báo: Không tìm thấy phần tử #chatButton.');
    }

    if (closeChatButton) {
        closeChatButton.addEventListener('click', closeChat);
    } else {
        console.warn('Cảnh báo: Không tìm thấy phần tử #closeChatButton.');
    }

    // Bấm vào Overlay để đóng Sidebar
    overlay.addEventListener('click', closeChat);


    // --- Logic Scroll To Top ---

    if (scrollTopButton) {
        // Ban đầu ẩn nút
        scrollTopButton.style.display = 'none';

        // Lắng nghe sự kiện cuộn
        window.addEventListener('scroll', () => {
            // Hiện nút khi cuộn xuống quá 300px
            if (document.body.scrollTop > 300 || document.documentElement.scrollTop > 300) {
                scrollTopButton.style.display = 'flex';
            } else {
                scrollTopButton.style.display = 'none';
            }
        });

        // Thêm sự kiện click cho nút
        scrollTopButton.addEventListener('click', () => {
            window.scrollTo({
                top: 0,
                behavior: 'smooth' // Cuộn mượt
            });
        });
    } else {
        console.warn('Cảnh báo: Không tìm thấy phần tử #scrollToTopButton.');
    }
});
