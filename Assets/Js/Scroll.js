// Lấy phần tử nút
const scrollTopButton = document.getElementById('scrollToTopButton');

// Lắng nghe sự kiện cuộn
window.addEventListener('scroll', () => {
    // Nếu trang cuộn xuống hơn 300px, hiện nút
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

// Ban đầu ẩn nút nếu chưa cuộn
scrollTopButton.style.display = 'none';

// Hàm xử lý khi bấm nút Chat (để dành cho code xử lý sau)
document.getElementById('chatButton').addEventListener('click', () => {
    console.log('Nút Chat đã được bấm. Thêm logic chat tại đây.');
    // Ví dụ: Mở Modal Chat, hoặc gọi hàm chat API
});

// ============================================================================================================

