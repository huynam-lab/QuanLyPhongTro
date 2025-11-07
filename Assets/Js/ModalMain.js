
// =======================================================
// LOGIC CHUNG CHO 2 MODAL
// =======================================================

const locationDisplay = document.getElementById('locationDisplay');
const selectedLocationsInput = document.getElementById('selectedLocationsInput');
const locationListContainer = document.getElementById('locationListContainer');
const locationModalElement = document.getElementById('locationModal');
const filterModalElement = document.getElementById('filterModal');

// Khởi tạo Modal instances
const locationModal = new bootstrap.Modal(locationModalElement);
const filterModal = new bootstrap.Modal(filterModalElement);

// Dữ liệu mẫu 20 khu vực
//const locationsData = [
//    { id: 'all', name: 'Toàn quốc', isSelected: true },
//    { id: 'hcm', name: 'Hồ Chí Minh', isSelected: false },
//    { id: 'hn', name: 'Hà Nội', isSelected: false },
//    { id: 'dn', name: 'Đà Nẵng', isSelected: false },
//    { id: 'bd', name: 'Bình Dương', isSelected: false },
//    { id: 'dnai', name: 'Đồng Nai', isSelected: false },
//    { id: 'bvt', name: 'Bà Rịa - Vũng Tàu', isSelected: false },
//    { id: 'ct', name: 'Cần Thơ', isSelected: false },
//    { id: 'kh', name: 'Khánh Hòa', isSelected: false },
//    { id: 'hp', name: 'Hải Phòng', isSelected: false },
//    { id: 'ag', name: 'An Giang', isSelected: false },
//    { id: 'gl', name: 'Gia Lai', isSelected: false },
//    { id: 'kg', name: 'Kiên Giang', isSelected: false },
//    { id: 'lca', name: 'Lào Cai', isSelected: false },
//    { id: 'nt', name: 'Ninh Thuận', isSelected: false },
//    { id: 'py', name: 'Phú Yên', isSelected: false },
//    { id: 'qn', name: 'Quảng Ninh', isSelected: false },
//    { id: 'tg', name: 'Tiền Giang', isSelected: false },
//    { id: 'vt', name: 'Vĩnh Phúc', isSelected: false },
//    { id: 'la', name: 'Long An', isSelected: false }
//];


// --- Dữ liệu Bộ lọc (Modal 2) ---
let currentFilters = {
    category: 'phongtro',
    price: 'all',
    area: ['all'], // Diện tích cho phép chọn nhiều
    utilities: []
};

// =======================================================
// LOGIC MODAL TÌM KIẾM KHU VỰC (VẪN GIỮ NGUYÊN)
// =======================================================

function showLocationModal() {
    locationModal.show();
}

//function renderLocationList() {
//    locationListContainer.innerHTML = '';
//    locationsData.forEach(loc => {
//        const isChecked = loc.isSelected ? 'checked' : '';
//        const itemDiv = document.createElement('div');
//        itemDiv.className = 'location-item';
//        itemDiv.innerHTML = `
//                    <label class="d-flex align-items-center w-100">
//                        <input type="checkbox" 
//                                class="form-check-input flex-shrink-0" 
//                                data-id="${loc.id}" 
//                                ${isChecked}>
//                        <span class="ms-3 text-dark">${loc.name}</span>
//                    </label>
//                    <i class="bi bi-chevron-right location-arrow"></i>
//                `;

//        itemDiv.addEventListener('click', function (e) {
//            const checkbox = itemDiv.querySelector('input[type="checkbox"]');
//            if (e.target.tagName !== 'INPUT' && e.target.tagName !== 'SPAN') {
//                checkbox.checked = !checkbox.checked;
//            }
//            handleLocationCheckboxChange(checkbox.dataset.id, checkbox.checked);
//        });

//        itemDiv.querySelector('input[type="checkbox"]').addEventListener('change', function (e) {
//            handleLocationCheckboxChange(e.target.dataset.id, e.target.checked);
//        });

//        locationListContainer.appendChild(itemDiv);
//    });
//}
let locationsState = [];
function initializeLocationState() {
    locationsState = [];
    // Lấy tất cả các checkbox trong modal
    const checkboxes = document.querySelectorAll('#locationListContainer input[type="checkbox"]');
    checkboxes.forEach(cb => {
        locationsState.push({
            id: cb.dataset.id,
            name: cb.dataset.name || (cb.dataset.id === 'all' ? 'Toàn quốc' : 'Tên không xác định'),
            isSelected: cb.checked
        });

        // Thêm Event Listener cho từng checkbox
        cb.addEventListener('change', function (e) {
            handleLocationCheckboxChange(e.target.dataset.id, e.target.checked);
        });
    });

    // Cần phải khởi tạo lại Event Listener cho các div cha (location-item)
    document.querySelectorAll('.location-item').forEach(itemDiv => {
        itemDiv.addEventListener('click', function (e) {
            const checkbox = itemDiv.querySelector('input[type="checkbox"]');
            // Đảm bảo không xử lý khi click trực tiếp vào checkbox/label text
            if (e.target.tagName !== 'INPUT' && e.target.tagName !== 'SPAN') {
                checkbox.checked = !checkbox.checked;
                handleLocationCheckboxChange(checkbox.dataset.id, checkbox.checked);
            }
        });
    });
}
function handleLocationCheckboxChange(locationId, isChecked) {
    // 1. Cập nhật trạng thái trong mảng JS
    const locationItem = locationsState.find(loc => loc.id === locationId);
    if (locationItem) {
        locationItem.isSelected = isChecked;
    }

    // 2. Xử lý logic Toàn quốc/Khu vực cụ thể (giống logic cũ)
    if (locationId === 'all') {
        if (isChecked) {
            locationsState.forEach(loc => {
                if (loc.id !== 'all') {
                    loc.isSelected = false;
                }
            });
        }
    } else {
        if (isChecked) {
            const allItem = locationsState.find(loc => loc.id === 'all');
            if (allItem) allItem.isSelected = false;
        }
    }

    // 3. Đảm bảo ít nhất một mục được chọn (nếu không thì chọn 'all')
    const checkedCount = locationsState.filter(loc => loc.isSelected).length;
    if (checkedCount === 0) {
        const allItem = locationsState.find(loc => loc.id === 'all');
        if (allItem) allItem.isSelected = true;
    }

    // 4. Đồng bộ lại trạng thái từ mảng JS ra DOM (rất quan trọng)
    locationsState.forEach(loc => {
        const checkbox = document.querySelector(`#locationListContainer input[data-id="${loc.id}"]`);
        if (checkbox) {
            // Chỉ đồng bộ nếu trạng thái trong JS khác trạng thái DOM, 
            // tránh lặp vô tận nếu có logic phức tạp
            if (checkbox.checked !== loc.isSelected) {
                checkbox.checked = loc.isSelected;
            }
        }
    });
}

function updateLocationDisplayField() {
    const activeLocations = locationsState
        .filter(loc => loc.isSelected && loc.id !== 'all')
        .map(loc => loc.name);

    // ... (phần còn lại của hàm giữ nguyên logic sử dụng activeLocations) ...

    if (activeLocations.length === 0) {
        locationDisplay.value = 'Tìm theo khu vực';
        selectedLocationsInput.value = '';
        // Đảm bảo 'all' được chọn lại trong state
        const allItem = locationsState.find(loc => loc.id === 'all');
        if (allItem) {
            allItem.isSelected = true;
            document.querySelector('#locationListContainer input[data-id="all"]').checked = true;
        }
    } else {
        const displayNames = activeLocations.slice(0, 3).join(', ');
        // ... (giữ nguyên phần tính toán display text)
        const remainingCount = activeLocations.length - 3;

        let displayText = displayNames;
        if (remainingCount > 0) {
            displayText += ` +${remainingCount} khu vực khác`;
        }

        locationDisplay.value = displayText;
        selectedLocationsInput.value = activeLocations.join(',');
    }
}

// Lắng nghe sự kiện Modal Khu vực đóng
locationModalElement.addEventListener('hidden.bs.modal', function () {
    updateLocationDisplayField();
});
document.addEventListener('DOMContentLoaded', function () {
    initializeLocationState(); // Thay thế renderLocationList()
    updateLocationDisplayField();

    document.getElementById('applyLocationFilter').addEventListener('click', function () {
        updateLocationDisplayField();
    });
});


// =======================================================
// LOGIC MODAL BỘ LỌC (FILTER MODAL) - MỚI
// =======================================================

function showFilterModal() {
    // Khi mở Modal, đảm bảo giao diện Modal đồng bộ với state hiện tại
    syncFilterModalUI();
    filterModal.show();
}

function syncFilterModalUI() {
    // 1. Đồng bộ Danh mục
    document.querySelectorAll('.category-item').forEach(btn => {
        btn.classList.remove('active');
        if (btn.dataset.category === currentFilters.category) {
            btn.classList.add('active');
        }
    });

    // 2. Đồng bộ Khoảng giá (Radio style - chỉ chọn 1)
    document.querySelectorAll('.tag-group button[data-price-range]').forEach(btn => {
        btn.classList.remove('active');
        if (btn.dataset.priceRange === currentFilters.price) {
            btn.classList.add('active');
        }
    });

    // 3. Đồng bộ Khoảng diện tích (Checkbox style - có thể chọn nhiều)
    document.querySelectorAll('.tag-group button[data-area-range]').forEach(btn => {
        if (currentFilters.area.includes(btn.dataset.areaRange)) {
            btn.classList.add('active');
        } else {
            btn.classList.remove('active');
        }
    });

    // 4. Đồng bộ Đặc điểm nổi bật (Checkbox style - có thể chọn nhiều)
    document.querySelectorAll('.tag-group button[data-utility]').forEach(btn => {
        if (currentFilters.utilities.includes(btn.dataset.utility)) {
            btn.classList.add('active');
        } else {
            btn.classList.remove('active');
        }
    });
}

// --- Event Listeners cho các nhóm lọc ---

// 1. Danh mục cho thuê
document.querySelectorAll('.category-item').forEach(item => {
    item.addEventListener('click', function () {
        // Đặt lại state và cập nhật UI
        currentFilters.category = item.dataset.category;
        syncFilterModalUI();
    });
});

// 2. Khoảng giá (Radio logic)
document.querySelectorAll('.tag-group button[data-price-range]').forEach(btn => {
    btn.addEventListener('click', function () {
        // Đặt lại state và cập nhật UI
        currentFilters.price = btn.dataset.priceRange;
        syncFilterModalUI();
    });
});

// 3. Khoảng diện tích (Checkbox logic)
document.querySelectorAll('.tag-group button[data-area-range]').forEach(btn => {
    btn.addEventListener('click', function () {
        const range = btn.dataset.areaRange;

        if (range === 'all') {
            // Nếu click "Tất cả", đặt lại chỉ còn 'all'
            currentFilters.area = ['all'];
        } else {
            // Nếu click range cụ thể, loại bỏ 'all' nếu nó đang có
            currentFilters.area = currentFilters.area.filter(a => a !== 'all');

            const index = currentFilters.area.indexOf(range);
            if (index > -1) {
                currentFilters.area.splice(index, 1); // Bỏ chọn
            } else {
                currentFilters.area.push(range); // Chọn
            }

            // Nếu không còn gì được chọn, tự động chọn lại 'all'
            if (currentFilters.area.length === 0) {
                currentFilters.area.push('all');
            }
        }
        syncFilterModalUI();
    });
});

// 4. Đặc điểm nổi bật (Checkbox logic)
document.querySelectorAll('.tag-group button[data-utility]').forEach(btn => {
    btn.addEventListener('click', function () {
        const utility = btn.dataset.utility;
        const index = currentFilters.utilities.indexOf(utility);

        if (index > -1) {
            currentFilters.utilities.splice(index, 1); // Bỏ chọn
        } else {
            currentFilters.utilities.push(utility); // Chọn
        }
        syncFilterModalUI();
    });
});

// Hàm Áp dụng Bộ lọc (gọi khi bấm nút Áp dụng)
function applyFilterAndClose() {
    console.log("Filters đã áp dụng:", currentFilters);
    // Ở đây bạn có thể thêm logic gửi API/reload trang với các filter
    filterModal.hide();
}

// --- Khởi tạo khi DOM sẵn sàng ---
document.addEventListener('DOMContentLoaded', function () {
    renderLocationList();
    updateLocationDisplayField();

    document.getElementById('applyLocationFilter').addEventListener('click', function () {
        updateLocationDisplayField();
    });
});