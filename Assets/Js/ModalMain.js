// =======================================================
// CÁC BIẾN DOM
// =======================================================

const locationDisplay = document.getElementById('locationDisplay');
const selectedLocationsInput = document.getElementById('selectedLocationsInput');
const locationListContainer = document.getElementById('locationListContainer');

const locationModalElement = document.getElementById('locationModal');
const filterModalElement = document.getElementById('filterModal');

const locationModal = new bootstrap.Modal(locationModalElement);
const filterModal = new bootstrap.Modal(filterModalElement);

// =======================================================
// STATE KHU VỰC
// =======================================================

let locationsState = []; // {id, name, isSelected}

// Hàm mở modal nơi
function showLocationModal() {
    locationModal.show();
}

// Khởi tạo state từ checkbox
function initializeLocationState() {
    locationsState = [];

    const checkboxes = document.querySelectorAll('#locationListContainer input[type="checkbox"]');

    checkboxes.forEach(cb => {
        locationsState.push({
            id: cb.dataset.id,
            name: cb.dataset.name || 'Không tên',
            isSelected: cb.checked
        });

        // Lắng nghe thay đổi
        cb.addEventListener('change', function () {
            handleLocationCheckboxChange(cb.dataset.id, cb.checked);
        });
    });

    // Click vào dòng ngoài checkbox cũng chọn
    document.querySelectorAll('.location-item').forEach(div => {
        div.addEventListener('click', function (e) {
            const cb = div.querySelector('input[type="checkbox"]');
            if (e.target.tagName !== 'INPUT' && e.target.tagName !== 'SPAN') {
                cb.checked = !cb.checked;
                handleLocationCheckboxChange(cb.dataset.id, cb.checked);
            }
        });
    });
}

function handleLocationCheckboxChange(id, isChecked) {

    // Cập nhật state
    const item = locationsState.find(x => x.id === id);
    if (item) item.isSelected = isChecked;

    // Nếu chọn "all"
    if (id === "all" && isChecked) {
        locationsState.forEach(loc => {
            if (loc.id !== 'all') loc.isSelected = false;
        });
    }

    // Nếu tick khu vực -> bỏ "all"
    if (id !== "all" && isChecked) {
        const allItem = locationsState.find(x => x.id === 'all');
        if (allItem) allItem.isSelected = false;
    }

    // Không chọn gì -> auto chọn all
    if (!locationsState.some(x => x.isSelected)) {
        const all = locationsState.find(x => x.id === 'all');
        if (all) all.isSelected = true;
    }

    // Đồng bộ checkbox lại theo state
    locationsState.forEach(loc => {
        const dom = document.querySelector(`#locationListContainer input[data-id="${loc.id}"]`);
        if (dom) dom.checked = loc.isSelected;
    });
}

// Cập nhật ô hiển thị
function updateLocationDisplayField() {
    const list = locationsState
        .filter(l => l.isSelected && l.id !== "all")
        .map(l => l.name);

    if (list.length === 0) {
        locationDisplay.value = "Tìm theo khu vực";
        selectedLocationsInput.value = "";
        return;
    }

    const display = list.slice(0, 3).join(", ");
    const more = list.length - 3;

    locationDisplay.value = more > 0 ? `${display} +${more} khu vực khác` : display;
    selectedLocationsInput.value = list.join(",");
}

// Modal đóng → cập nhật hiển thị
locationModalElement.addEventListener('hidden.bs.modal', updateLocationDisplayField);


// =======================================================
// NÚT ÁP DỤNG KHU VỰC
// =======================================================

function applyLocationFilter() {

    const selected = locationsState
        .filter(l => l.isSelected && l.id !== "all")
        .map(l => l.id);

    let query = "";

    if (selected.length > 0) {
        query = selected.map(id => `kvIds=${id}`).join("&");
    }

    locationModal.hide();

    window.location.href = "/User/TimKiem?" + query;
}


// =======================================================
// FILTER MODAL
// =======================================================

let currentFilters = {
    category: "phongtro",
    price: "all",
    area: ["all"],
    utilities: []
};

function showFilterModal() {
    syncFilterModalUI();
    filterModal.show();
}

function syncFilterModalUI() {

    document.querySelectorAll('.category-item').forEach(btn => {
        btn.classList.toggle("active", btn.dataset.category === currentFilters.category);
    });

    document.querySelectorAll('button[data-price-range]').forEach(btn => {
        btn.classList.toggle("active", btn.dataset.priceRange === currentFilters.price);
    });

    document.querySelectorAll('button[data-area-range]').forEach(btn => {
        btn.classList.toggle("active", currentFilters.area.includes(btn.dataset.areaRange));
    });

    document.querySelectorAll('button[data-utility]').forEach(btn => {
        btn.classList.toggle("active", currentFilters.utilities.includes(btn.dataset.utility));
    });
}

// Danh mục
document.querySelectorAll('.category-item').forEach(btn => {
    btn.addEventListener("click", () => {
        currentFilters.category = btn.dataset.category;
        syncFilterModalUI();
    });
});

// Giá
document.querySelectorAll('button[data-price-range]').forEach(btn => {
    btn.addEventListener("click", () => {
        currentFilters.price = btn.dataset.priceRange;
        syncFilterModalUI();
    });
});

// Diện tích
document.querySelectorAll('button[data-area-range]').forEach(btn => {
    btn.addEventListener("click", () => {
        const val = btn.dataset.areaRange;

        if (val === "all") currentFilters.area = ["all"];
        else {
            currentFilters.area = currentFilters.area.filter(a => a !== "all");

            if (currentFilters.area.includes(val))
                currentFilters.area = currentFilters.area.filter(a => a !== val);
            else
                currentFilters.area.push(val);

            if (currentFilters.area.length === 0)
                currentFilters.area = ["all"];
        }

        syncFilterModalUI();
    });
});

// Tiện ích
document.querySelectorAll('button[data-utility]').forEach(btn => {
    btn.addEventListener("click", () => {
        const ut = btn.dataset.utility;

        if (currentFilters.utilities.includes(ut))
            currentFilters.utilities = currentFilters.utilities.filter(x => x !== ut);
        else
            currentFilters.utilities.push(ut);

        syncFilterModalUI();
    });
});

function applyFilterAndClose() {
    console.log("Filters:", currentFilters);
    filterModal.hide();
}


// =======================================================
// KHỞI TẠO
// =======================================================

document.addEventListener("DOMContentLoaded", () => {
    initializeLocationState();
    updateLocationDisplayField();

    document.getElementById("applyLocationFilter")
        .addEventListener("click", applyLocationFilter);
});
