/**
 * address-dropdowns.js
 * Xử lý các dropdown Tỉnh/Thành phố và Quận/Huyện phụ thuộc lẫn nhau.
 */

/**
 * Khởi tạo chức năng cho các dropdown địa chỉ phụ thuộc.
 * @param {object} config Đối tượng cấu hình.
 * @param {string} config.citySelectId ID của thẻ select Tỉnh/Thành phố.
 * @param {string} config.districtSelectId ID của thẻ select Quận/Huyện.
 * @param {string} config.apiUrlTemplate Mẫu URL để lấy danh sách quận/huyện, với '__CITY_ID__' làm placeholder cho ID thành phố. Ví dụ: '/Controller/Action?id=__CITY_ID__'
 * @param {string} [config.loadingIndicatorId] (Tùy chọn) ID của phần tử hiển thị/ẩn trong khi tải.
 * @param {object} [config.jsonPropertyNames] (Tùy chọn) Xác định tên thuộc tính mong đợi trong JSON trả về từ API. Mặc định là { value: 'id', text: 'ten' }.
 * @param {object} [config.logMessages] (Tùy chọn) Xác định các thông báo log tùy chỉnh. Các giá trị mặc định được cung cấp.
 */
function initializeAddressDropdowns(config) {
    // --- Cấu hình Mặc định ---
    const defaults = {
        loadingIndicatorId: null,
        jsonPropertyNames: { value: 'id', text: 'ten' }, // Thuộc tính mặc định trong JSON trả về
        logMessages: { // Các thông báo log mặc định (có thể ghi đè)
            initStart: "[AddressDropdowns] Đang khởi tạo...",
            initComplete: "[AddressDropdowns] Khởi tạo hoàn tất.",
            elementsNotFound: "[AddressDropdowns] Lỗi: Không tìm thấy các thẻ select yêu cầu.",
            apiUrlError: "[AddressDropdowns] Lỗi: Mẫu URL API không hợp lệ hoặc bị thiếu.",
            fetchStart: "[AddressDropdowns] Đang lấy quận/huyện cho Thành phố ID:",
            fetchApiUrl: "[AddressDropdowns] Đang gọi API:",
            fetchSuccess: "[AddressDropdowns] Dữ liệu API trả về:",
            fetchError: "[AddressDropdowns] Lỗi khi lấy hoặc xử lý dữ liệu quận/huyện:",
            noCitySelected: "[AddressDropdowns] Không có ID Thành phố hợp lệ được cung cấp.",
            noDistrictsFound: "[AddressDropdowns] Không tìm thấy quận/huyện nào cho Thành phố ID:",
            populatedDistricts: "[AddressDropdowns] Số lượng quận/huyện đã thêm:",
            preSelectedDistrict: "[AddressDropdowns] ID Quận/Huyện được chọn lại:",
            preSelectNotFound: "[AddressDropdowns] Cảnh báo: Không thể chọn lại ID Quận/Huyện vì không tìm thấy:"
        }
    };

    // Gộp cấu hình người dùng với mặc định
    const settings = { ...defaults, ...config };
    settings.jsonPropertyNames = { ...defaults.jsonPropertyNames, ...config.jsonPropertyNames };
    settings.logMessages = { ...defaults.logMessages, ...config.logMessages };

    console.log(settings.logMessages.initStart);

    // --- Lấy các phần tử DOM ---
    const citySelect = document.getElementById(settings.citySelectId);
    const districtSelect = document.getElementById(settings.districtSelectId);
    const districtLoading = settings.loadingIndicatorId ? document.getElementById(settings.loadingIndicatorId) : null;

    // --- Kiểm tra cơ bản ---
    // Kiểm tra sự tồn tại của các dropdown chính
    if (!citySelect || !districtSelect) {
        console.error(settings.logMessages.elementsNotFound, `City ID: ${settings.citySelectId}, District ID: ${settings.districtSelectId}`);
        return; // Không thể tiếp tục nếu thiếu element cốt lõi
    }
    // Kiểm tra mẫu URL API
    if (!settings.apiUrlTemplate || !settings.apiUrlTemplate.includes('__CITY_ID__')) {
        console.error(settings.logMessages.apiUrlError, settings.apiUrlTemplate);
        return; // Không thể tiếp tục nếu URL không hợp lệ
    }
    // Cảnh báo nếu không tìm thấy loading indicator (nhưng vẫn có thể tiếp tục)
    if (settings.loadingIndicatorId && !districtLoading) {
        console.warn(`[AddressDropdowns] Không tìm thấy phần tử loading indicator với ID '${settings.loadingIndicatorId}'.`);
    }

    // Lưu trữ ID quận/huyện được chọn ban đầu (quan trọng khi trang tải lại do lỗi validation)
    const previouslySelectedDistrictId = districtSelect.value || null;

    /**
     * Lấy dữ liệu quận/huyện và cập nhật dropdown.
     */
    async function fetchAndPopulateDistricts(selectedCityId, districtIdToSelect = null) {
        // --- KIỂM TRA ID THÀNH PHỐ NGHIÊM NGẶT ---
        // Chuyển sang chuỗi, cắt khoảng trắng và kiểm tra xem có phải ID hợp lệ không (khác null, rỗng, undefined, hoặc '0')
        // Giả định ID hợp lệ là số nguyên dương.
        const cityIdStr = selectedCityId ? String(selectedCityId).trim() : "";
        const isValidCityId = cityIdStr && /^[1-9]\d*$/.test(cityIdStr); // Kiểm tra có phải chuỗi số nguyên dương không

        console.log(settings.logMessages.fetchStart, cityIdStr || 'Không có', "| Đang cố chọn Quận/Huyện:", districtIdToSelect || 'Không có');

        // Nếu ID thành phố không hợp lệ
        if (!isValidCityId) {
            console.log(settings.logMessages.noCitySelected, `(Nhận được: '${selectedCityId}', không hợp lệ)`);
            // Reset và vô hiệu hóa dropdown quận/huyện ngay lập tức
            districtSelect.innerHTML = '<option value="">-- Chọn Quận/Huyện --</option>';
            districtSelect.disabled = true;
            if (districtLoading) districtLoading.style.display = 'none'; // Ẩn loading
            // Tùy chọn: Kích hoạt cập nhật validation (nếu dùng jQuery Validation) để xóa lỗi cũ
             if (typeof(jQuery) !== 'undefined' && jQuery.fn.valid && jQuery(districtSelect).valid) {
                 jQuery(districtSelect).valid();
             }
            return; // *** QUAN TRỌNG: Dừng thực thi nếu ID Thành phố không hợp lệ ***
        }
        // --- KẾT THÚC KIỂM TRA ---

        // Nếu đến được đây, cityIdStr được coi là hợp lệ để gọi API.

        // Hiển thị trạng thái đang tải TRƯỚC KHI gọi API
        districtSelect.innerHTML = '<option value="">-- Đang tải... --</option>';
        districtSelect.disabled = true; // Giữ trạng thái vô hiệu hóa khi đang tải
        if (districtLoading) districtLoading.style.display = 'inline-flex'; // Hiện loading

        // Xây dựng URL API thực tế
        const url = settings.apiUrlTemplate.replace('__CITY_ID__', encodeURIComponent(cityIdStr)); // Sử dụng ID chuỗi đã được kiểm tra
        console.log(settings.logMessages.fetchApiUrl, url); // Ghi log ngay trước khi fetch

        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`Phản hồi mạng không ổn: ${response.status} ${response.statusText}`);
            }
            const data = await response.json();
            console.log(settings.logMessages.fetchSuccess, data);

            // Chỉ xóa các option cũ (trừ option mặc định) sau khi fetch thành công
            districtSelect.innerHTML = '<option value="">-- Chọn Quận/Huyện --</option>';
            let foundDistrictToSelect = false; // Cờ đánh dấu đã tìm thấy quận/huyện cần chọn lại chưa
            let districtCount = 0; // Đếm số quận/huyện

            // Nếu API trả về dữ liệu hợp lệ
            if (data && Array.isArray(data) && data.length > 0) {
                districtCount = data.length;
                data.forEach(district => {
                    // Lấy giá trị và text từ JSON (kiểm tra tên thuộc tính nếu cần)
                    const value = district[settings.jsonPropertyNames.value];
                    const text = district[settings.jsonPropertyNames.text];
                    // Bỏ qua nếu thiếu thuộc tính cần thiết
                     if (value === undefined || text === undefined) {
                        console.warn("[AddressDropdowns] Cảnh báo: Dữ liệu quận/huyện thiếu thuộc tính mong đợi:", district);
                        return; // Bỏ qua item này
                    }
                    // Tạo option mới
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = text;
                    // Kiểm tra nếu cần chọn lại quận/huyện này (khi load lại trang lỗi)
                     if (districtIdToSelect && value.toString() === districtIdToSelect.toString()) {
                        option.selected = true;
                        foundDistrictToSelect = true;
                        console.log(settings.logMessages.preSelectedDistrict, districtIdToSelect);
                    }
                    // Thêm option vào dropdown
                    districtSelect.appendChild(option);
                });
                districtSelect.disabled = false; // Kích hoạt lại dropdown chỉ khi có dữ liệu
            } else {
                // Không tìm thấy quận/huyện cho thành phố này
                console.log(settings.logMessages.noDistrictsFound, cityIdStr); // Ghi log với ID đã kiểm tra
                districtSelect.innerHTML = '<option value="">-- Không có Quận/Huyện --</option>';
                districtSelect.disabled = true; // Giữ trạng thái vô hiệu hóa
            }

             console.log(settings.logMessages.populatedDistricts, districtCount); // Ghi log số lượng quận/huyện

             // Ghi cảnh báo nếu không tìm thấy quận/huyện cần chọn lại
             if (districtIdToSelect && !foundDistrictToSelect) {
                console.warn(settings.logMessages.preSelectNotFound, districtIdToSelect);
            }

        } catch (error) {
            // Xử lý lỗi khi fetch hoặc xử lý JSON
            console.error(settings.logMessages.fetchError, error);
            districtSelect.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';
            districtSelect.disabled = true; // Giữ trạng thái vô hiệu hóa khi lỗi
        } finally {
            // Luôn ẩn loading indicator dù thành công hay thất bại
            if (districtLoading) districtLoading.style.display = 'none';
            // Tùy chọn: Kích hoạt cập nhật validation
             if (typeof (jQuery) !== 'undefined' && jQuery.fn.valid && jQuery(districtSelect).valid) {
                 jQuery(districtSelect).valid();
             }
        }
    }

    // --- Gắn sự kiện 'change' cho dropdown Thành phố ---
    citySelect.addEventListener('change', function () {
        // Khi người dùng tự đổi thành phố, không cần chọn lại quận/huyện cũ (truyền null)
        fetchAndPopulateDistricts(this.value, null);
    });

    // --- Xử lý khi tải trang lần đầu ---
    const initialCityId = citySelect.value;
    // Kiểm tra sơ bộ, hàm fetch sẽ kiểm tra chi tiết hơn
    if (initialCityId) {
        // Nếu có thành phố được chọn sẵn (do lỗi validation chẳng hạn),
        // thì gọi hàm fetch để tải quận/huyện và cố gắng chọn lại quận/huyện cũ.
        fetchAndPopulateDistricts(initialCityId, previouslySelectedDistrictId);
    } else {
        // Không có thành phố nào được chọn ban đầu, đảm bảo dropdown quận/huyện bị vô hiệu hóa
        districtSelect.disabled = true;
        if (districtLoading) districtLoading.style.display = 'none'; // Ẩn loading nếu có
    }

    console.log(settings.logMessages.initComplete);
}