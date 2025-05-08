/**
 * jobPostingForm.js
 * Version: Hoàn thiện - Sửa lỗi hiển thị tên Ngành nghề
 */

function initJobPostingForm() {
    console.log("[JobPostingForm] Initializing script...");

    // --- Lấy các element DOM cần thiết ---
    const form = document.querySelector('form[action*="Create"], form[action*="Edit"]');
    const thanhPhoDropdown = document.getElementById('thanhPhoDropdown');
    const quanHuyenDropdown = document.getElementById('quanHuyenDropdown');
    const quanHuyenLoading = document.getElementById('quanHuyenLoading');
    const nganhNgheSearchInput = document.getElementById('nganhNgheSearchInput');
    const availableTagsContainer = document.getElementById('availableTagsContainer');
    const selectedTagsContainer = document.getElementById('selectedTagsContainer');
    const hiddenCheckboxesContainer = document.getElementById('hiddenCheckboxesContainer');
    const noSelectedTagText = document.getElementById('noSelectedTagText');
    const lichLamViecContainer = document.getElementById('lichLamViecContainer');
    const addLichLamViecButton = document.getElementById('addLichLamViec');

    let allNganhNgheTagsData = [];
    let lichIndex = lichLamViecContainer ? lichLamViecContainer.querySelectorAll('.lich-lam-viec-row').length : 0;

    // --- 1. Xử lý Dropdown Quận/Huyện (Giữ nguyên code từ trước) ---
    async function loadQuanHuyen(thanhPhoId, selectedQuanHuyenId = null) { /* ... Giữ nguyên ... */
        if (!quanHuyenDropdown) { console.warn("[JobPostingForm] Quan Huyen Dropdown not found."); return; }
        const isValidThanhPhoId = thanhPhoId && parseInt(thanhPhoId) > 0;
        quanHuyenDropdown.innerHTML = '<option value="">-- Chọn Quận/Huyện --</option>';
        quanHuyenDropdown.disabled = true;
        if (!isValidThanhPhoId) { if (quanHuyenLoading) quanHuyenLoading.style.display = 'none'; return; }
        if (quanHuyenLoading) quanHuyenLoading.style.display = 'inline-block';
        try {
            const apiUrl = `/api/DiaChi/GetQuanHuyenByThanhPho?thanhPhoId=${thanhPhoId}`;
            const response = await fetch(apiUrl);
            if (!response.ok) throw new Error(`API Error: ${response.status}`);
            const data = await response.json();
            if (data && Array.isArray(data) && data.length > 0) {
                data.forEach(qh => {
                    const option = new Option(qh.ten, qh.id);
                    if (selectedQuanHuyenId && qh.id == selectedQuanHuyenId) option.selected = true;
                    quanHuyenDropdown.appendChild(option);
                });
                quanHuyenDropdown.disabled = false;
            } else { quanHuyenDropdown.innerHTML = '<option value="">-- Không có Quận/Huyện --</option>'; }
        } catch (error) { console.error('[JobPostingForm] Error loading Quan Huyen:', error); quanHuyenDropdown.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';}
        finally { if (quanHuyenLoading) quanHuyenLoading.style.display = 'none'; }
     }
    if (thanhPhoDropdown) {
        thanhPhoDropdown.addEventListener('change', function () { loadQuanHuyen(this.value); });
        if (thanhPhoDropdown.value && parseInt(thanhPhoDropdown.value) > 0) {
             const initialQuanHuyenId = quanHuyenDropdown ? quanHuyenDropdown.value : null;
             loadQuanHuyen(thanhPhoDropdown.value, initialQuanHuyenId);
         } else if(quanHuyenDropdown) { quanHuyenDropdown.disabled = true; }
    } else { if(quanHuyenDropdown) quanHuyenDropdown.disabled = true; }


    // --- 2. Xử lý Tag Ngành Nghề ---

    /**
     * Render các tag có sẵn và lưu dữ liệu vào mảng allNganhNgheTagsData.
     * *** ĐÃ SỬA ĐỂ LẤY ĐÚNG TÊN NGÀNH NGHỀ ***
     */
    function renderAndPrepareAvailableTags() {
        if (!availableTagsContainer || !hiddenCheckboxesContainer) {
            console.error("[JobPostingForm] Available tags or hidden checkboxes container not found.");
            return;
        }
        availableTagsContainer.innerHTML = '';
        allNganhNgheTagsData = [];

        const hiddenCheckboxes = hiddenCheckboxesContainer.querySelectorAll('.nganh-nghe-checkbox');
        if (!hiddenCheckboxes || hiddenCheckboxes.length === 0) {
            availableTagsContainer.innerHTML = '<span class="text-muted small">Không có dữ liệu ngành nghề.</span>';
            return;
        }

        // --- Truyền dữ liệu từ ViewBag vào JS ---
        // Cách 1: Render dữ liệu vào một script tag trong View (khuyến khích)
        // Trong _JobPostingFormPartial.cshtml, thêm vào cuối cùng (bên ngoài form):
        /*
        <script id="nganhNgheData" type="application/json">
            @if (ViewBag.NganhNgheList != null) {
                @Html.Raw(System.Text.Json.JsonSerializer.Serialize(((List<SelectListItem>)ViewBag.NganhNgheList).Select(i => new { id = i.Value, text = i.Text })))
            } else {
                @Html.Raw("[]")
            }
        </script>
        */
        // Sau đó đọc dữ liệu này trong JS:
        const nganhNgheDataElement = document.getElementById('nganhNgheData');
        let nganhNgheDataSource = [];
        if (nganhNgheDataElement) {
            try {
                nganhNgheDataSource = JSON.parse(nganhNgheDataElement.textContent || '[]');
                 console.log("[JobPostingForm] Loaded NganhNghe data from script tag:", nganhNgheDataSource);
            } catch (e) {
                 console.error("[JobPostingForm] Error parsing NganhNghe data from script tag:", e);
            }
        } else {
             console.warn("[JobPostingForm] Script tag with ID 'nganhNgheData' not found. Cannot get tag text reliably.");
             // Fallback: Lấy từ checkbox nếu không có script tag
             hiddenCheckboxes.forEach(cb => nganhNgheDataSource.push({ id: cb.value, text: `Ngành ${cb.value}` }));

        }


        // --- Tạo tag dựa trên dữ liệu đã lấy ---
         hiddenCheckboxes.forEach(checkbox => {
            const id = checkbox.value;
            const isChecked = checkbox.checked;
             // Tìm text tương ứng từ nguồn dữ liệu đã lấy
             const dataItem = nganhNgheDataSource.find(item => item.id === id);
             const text = dataItem ? dataItem.text : `Ngành ${id}`; // Lấy text hoặc fallback

            const tagData = { id: id, text: text, isSelected: isChecked, element: null };
            allNganhNgheTagsData.push(tagData);

            const tagElement = createAvailableTagElement(id, text, isChecked);
            tagData.element = tagElement;
            availableTagsContainer.appendChild(tagElement);
        });

        addAvailableTagClickListeners();
        console.log(`[JobPostingForm] Rendered ${allNganhNgheTagsData.length} available tags.`);
         // Gọi lại filter để ẩn các tag đã chọn ban đầu
         filterAvailableTags();
    }

    /** Tạo DOM element cho một tag có sẵn. (Giữ nguyên) */
    function createAvailableTagElement(id, text, isSelected) { /* ... Giữ nguyên ... */
        const tag = document.createElement('span');
        tag.className = `badge me-2 mb-2 p-2 rounded-pill available-tag ${isSelected ? 'bg-secondary text-white selected' : 'bg-light text-dark'}`;
        tag.dataset.id = id;
        tag.textContent = text;
        tag.setAttribute('role', 'button');
        tag.setAttribute('tabindex', '0');
        tag.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
        tag.style.display = isSelected ? 'none' : 'inline-block';
        return tag;
     }

    /** Tạo DOM element cho một tag đã chọn. (Giữ nguyên) */
    function createSelectedTagElement(id, text) { /* ... Giữ nguyên ... */
        const tag = document.createElement('span');
        tag.className = 'badge bg-primary me-2 mb-2 p-2 rounded-pill shadow-sm selected-tag';
        tag.dataset.id = id;
        tag.innerHTML = `${text} <i class="fas fa-times ms-1 remove-tag" role="button" tabindex="0" aria-label="Xóa tag ${text}" data-id="${id}"></i>`;
        const removeIcon = tag.querySelector('.remove-tag');
        if(removeIcon) {
            removeIcon.addEventListener('click', handleRemoveTagClick);
            removeIcon.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') handleRemoveTagClick(e); });
        }
        return tag;
     }

    /** Cập nhật hiển thị text "Chưa chọn...". (Giữ nguyên) */
    function updateNoSelectedTagTextVisibility() { /* ... Giữ nguyên ... */
        if (noSelectedTagText && selectedTagsContainer) {
            const hasSelectedTags = selectedTagsContainer.querySelector('.selected-tag') !== null;
            noSelectedTagText.style.display = hasSelectedTags ? 'none' : 'inline';
        }
     }

    /** Xử lý click vào tag có sẵn. (Giữ nguyên logic, nhưng text lấy từ tagData) */
    function handleAvailableTagClick(event) { /* ... Giữ nguyên logic chọn/bỏ chọn ... */
        const clickedTag = event.target.closest('.available-tag');
        if (!clickedTag) return;
        const id = clickedTag.dataset.id;
        const correspondingCheckbox = hiddenCheckboxesContainer?.querySelector(`#nn_${id}`);
        if (!correspondingCheckbox) return;

        const tagData = allNganhNgheTagsData.find(t => t.id == id);
        if (!tagData) return; // Không tìm thấy dữ liệu tag

        if (!correspondingCheckbox.checked) { // Chỉ xử lý khi chưa chọn
            correspondingCheckbox.checked = true;
            tagData.isSelected = true;
            if (selectedTagsContainer) {
                const newSelectedTag = createSelectedTagElement(id, tagData.text); // Lấy text từ tagData
                selectedTagsContainer.appendChild(newSelectedTag);
            }
            clickedTag.style.display = 'none';
            clickedTag.setAttribute('aria-pressed', 'true');
            updateNoSelectedTagTextVisibility();
            if (typeof(jQuery) !== 'undefined' && jQuery.validator) { jQuery(correspondingCheckbox).valid(); }
        }
     }

     /** Xử lý click vào nút xóa (x) trên tag đã chọn. (Giữ nguyên logic) */
     function handleRemoveTagClick(event) { /* ... Giữ nguyên logic ... */
        const removeIcon = event.target.closest('.remove-tag');
         if (!removeIcon) return;
         const id = removeIcon.dataset.id;
         const selectedTagToRemove = removeIcon.closest('.selected-tag');
         const correspondingCheckbox = hiddenCheckboxesContainer?.querySelector(`#nn_${id}`);
         const tagData = allNganhNgheTagsData.find(t => t.id == id); // Tìm tag data
         if (!selectedTagToRemove || !correspondingCheckbox || !tagData) return;

         correspondingCheckbox.checked = false;
         tagData.isSelected = false; // Cập nhật data
         selectedTagToRemove.remove();

         if (tagData.element) { // Hiển thị lại tag có sẵn
             tagData.element.style.display = 'inline-block';
             tagData.element.classList.remove('selected', 'bg-secondary', 'text-white');
             tagData.element.classList.add('bg-light', 'text-dark');
             tagData.element.setAttribute('aria-pressed', 'false');
         }
         updateNoSelectedTagTextVisibility();
         if (typeof(jQuery) !== 'undefined' && jQuery.validator) { jQuery(correspondingCheckbox).valid(); }
     }

    /** Gắn sự kiện click (delegation) cho container tag có sẵn. (Giữ nguyên) */
    function addAvailableTagClickListeners() { /* ... Giữ nguyên ... */
         if (!availableTagsContainer) return;
        availableTagsContainer.removeEventListener('click', handleAvailableTagClick);
        availableTagsContainer.addEventListener('click', handleAvailableTagClick);
        availableTagsContainer.removeEventListener('keydown', handleAvailableTagKeydown);
        availableTagsContainer.addEventListener('keydown', handleAvailableTagKeydown);
        // console.log("[JobPostingForm] Added listeners for available tags.");
     }
     function handleAvailableTagKeydown(event) { /* ... Giữ nguyên ... */
        if (event.key === 'Enter' || event.key === ' ') {
             const focusedTag = event.target.closest('.available-tag');
             if (focusedTag) { event.preventDefault(); handleAvailableTagClick(event); }
         }
      }

    /** Lọc danh sách tag có sẵn dựa trên input tìm kiếm. (Giữ nguyên logic) */
    function filterAvailableTags() { /* ... Giữ nguyên logic ... */
        if (!nganhNgheSearchInput || !availableTagsContainer) return;
        const filterText = nganhNgheSearchInput.value.toLowerCase().trim();
        allNganhNgheTagsData.forEach(tagData => {
            if (tagData.element) {
                const tagText = tagData.text.toLowerCase();
                const isMatch = filterText === '' || tagText.includes(filterText);
                tagData.element.style.display = (isMatch && !tagData.isSelected) ? 'inline-block' : 'none';
            }
        });
    }

    // Khởi tạo phần Ngành nghề Tag
    if (nganhNgheSearchInput && availableTagsContainer && selectedTagsContainer && hiddenCheckboxesContainer) {
        renderAndPrepareAvailableTags(); // Quan trọng: Render và chuẩn bị data trước
        nganhNgheSearchInput.addEventListener('input', filterAvailableTags);
        // Gắn sự kiện cho các nút xóa có sẵn ban đầu (Edit)
        selectedTagsContainer.querySelectorAll('.remove-tag').forEach(icon => {
            icon.addEventListener('click', handleRemoveTagClick);
            icon.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') handleRemoveTagClick(e); });
        });
        updateNoSelectedTagTextVisibility();
        console.log("[JobPostingForm] Tag section initialized.");
    } else { console.warn("[JobPostingForm] Nganh Nghe Tag elements not found."); }


    // --- 3. Xử lý Thêm/Xóa Lịch làm việc (Giữ nguyên code từ trước) ---
    function addLichLamViecRow() { /* ... Giữ nguyên ... */
        if (!lichLamViecContainer) { console.error("[JobPostingForm] LichLamViec Container not found."); return; }
        const sampleRow = lichLamViecContainer.querySelector('.lich-lam-viec-row');
        let optionsNgayHtml = '<option value="">-- Ngày --</option>';
        let optionsBuoiHtml = '<option value="">-- Buổi --</option>';
        if (sampleRow) { const sampleSelectNgay = sampleRow.querySelector('select[name$=".NgayTrongTuan"]'); const sampleSelectBuoi = sampleRow.querySelector('select[name$=".BuoiLamViec"]'); if (sampleSelectNgay) optionsNgayHtml = sampleSelectNgay.innerHTML; if (sampleSelectBuoi) optionsBuoiHtml = sampleSelectBuoi.innerHTML; }
        const newRow = document.createElement('div'); newRow.className = 'row g-2 mb-2 align-items-center lich-lam-viec-row'; const currentIndex = lichIndex;
        newRow.innerHTML = `<input type="hidden" name="LichLamViecItems[${currentIndex}].Id" value="" /><input type="hidden" name="LichLamViecItems[${currentIndex}].MarkedForDeletion" value="false" class="mark-delete-flag"/><div class="col-md-3"><select name="LichLamViecItems[${currentIndex}].NgayTrongTuan" class="form-select form-select-sm">${optionsNgayHtml}</select><span class="text-danger field-validation-valid small" data-valmsg-for="LichLamViecItems[${currentIndex}].NgayTrongTuan" data-valmsg-replace="true"></span></div><div class="col-md-3"><select name="LichLamViecItems[${currentIndex}].BuoiLamViec" class="form-select form-select-sm">${optionsBuoiHtml}</select><span class="text-danger field-validation-valid small" data-valmsg-for="LichLamViecItems[${currentIndex}].BuoiLamViec" data-valmsg-replace="true"></span></div><div class="col-md-2"><input name="LichLamViecItems[${currentIndex}].GioBatDau" type="time" class="form-control form-control-sm"><span class="text-danger field-validation-valid small" data-valmsg-for="LichLamViecItems[${currentIndex}].GioBatDau" data-valmsg-replace="true"></span></div><div class="col-md-2"><input name="LichLamViecItems[${currentIndex}].GioKetThuc" type="time" class="form-control form-control-sm"><span class="text-danger field-validation-valid small" data-valmsg-for="LichLamViecItems[${currentIndex}].GioKetThuc" data-valmsg-replace="true"></span></div><div class="col-md-2 text-end"><button type="button" class="btn btn-sm btn-outline-danger remove-lich-lam-viec" title="Xóa dòng lịch"><i class="fas fa-times"></i></button></div>`;
        lichLamViecContainer.appendChild(newRow); lichIndex++; addRemoveEventListenersLich();
        if (form && typeof(jQuery) !== 'undefined' && jQuery.validator && jQuery.validator.unobtrusive) { jQuery.validator.unobtrusive.parse(form); }
        console.log(`[JobPostingForm] Added LichLamViec row index ${currentIndex}`);
     }
    function removeLichLamViecRow(button) { /* ... Giữ nguyên ... */
        const row = button.closest('.lich-lam-viec-row'); if (row && lichLamViecContainer) { const idInput = row.querySelector('input[type="hidden"][name$=".Id"]'); const deleteFlagInput = row.querySelector('.mark-delete-flag'); if (idInput && idInput.value && idInput.value !== "0" && deleteFlagInput) { deleteFlagInput.value = "true"; row.style.display = 'none'; } else if (deleteFlagInput) { row.remove(); } }
     }
    function addRemoveEventListenersLich() { /* ... Giữ nguyên ... */
        if (!lichLamViecContainer) return; const currentRemoveButtons = lichLamViecContainer.querySelectorAll('.remove-lich-lam-viec'); currentRemoveButtons.forEach(button => { const newButton = button.cloneNode(true); button.parentNode.replaceChild(newButton, button); newButton.addEventListener('click', function() { removeLichLamViecRow(this); }); newButton.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') removeLichLamViecRow(newButton); }); });
    }
    if (addLichLamViecButton) { addLichLamViecButton.addEventListener('click', addLichLamViecRow); }
    addRemoveEventListenersLich(); // Gọi lần đầu


    console.log("[JobPostingForm] Initialization complete.");
}

// --- Chạy hàm khởi tạo sau khi DOM đã tải xong ---
document.addEventListener('DOMContentLoaded', initJobPostingForm);